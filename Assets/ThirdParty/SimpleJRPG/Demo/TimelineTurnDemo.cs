using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SimpleJRPG;

namespace SimpleJRPG.Demo
{
    public class TimelineTurnDemo : MonoBehaviour
    {
        public string hubSceneName;

        // ── UI References (set by generator) ──

        [Header("Message Log")]
        public Transform messageContent;
        public ScrollRect messageScrollRect;

        [Header("Enemy Portraits")]
        public Image[] enemyPortraits;
        public TextMeshProUGUI[] enemyNameTexts;
        public Slider[] enemyHPBars;
        public TextMeshProUGUI[] enemyHPTexts;

        [Header("Party Portraits")]
        public Image[] partyPortraits;
        public TextMeshProUGUI[] partyNameTexts;
        public Slider[] partyHPBars;
        public Slider[] partyMPBars;
        public TextMeshProUGUI[] partyHPTexts;
        public TextMeshProUGUI[] partyMPTexts;

        [Header("Commands")]
        public Button btnFight;
        public Button btnHeal;
        public Button btnDefend;
        public Button btnRevive;
        public Button btnFlee;
        public GameObject commandPanel;

        [Header("Target Selection")]
        public Button[] targetButtons;
        public Button btnBack;

        [Header("Turn Indicators")]
        public GameObject[] partySelectMarks;
        public GameObject[] enemySelectMarks;

        [Header("Timeline")]
        public TextMeshProUGUI[] timelineSlots;

        // ── Action Costs ──

        private const int CostFight = 100;
        private const int CostHeal = 80;
        private const int CostDefend = 50;
        private const int CostRevive = 150;
        private const int CostEnemy = 100;

        // ── State ──

        private Battle _battle;
        private TimelineTurnSystem _timeline;
        private List<MockCombatant> _party = new List<MockCombatant>();
        private List<MockCombatant> _enemies = new List<MockCombatant>();
        private int _logCount;
        private bool _waitingForCommand;
        private int _actionCost = 100;

        private static readonly Color GoldColor = new Color(1f, 0.85f, 0.3f);

        void Start()
        {
            _party.Add(new MockCombatant("Hero", 120, 30, 10f, 0));
            _party.Add(new MockCombatant("Warrior", 100, 0, 8f, 0));
            _party.Add(new MockCombatant("Mage", 60, 80, 12f, 0));
            _party.Add(new MockCombatant("Priest", 80, 60, 9f, 0));

            _enemies.Add(new MockCombatant("Blob", 30, 0, 5f, 1));
            _enemies.Add(new MockCombatant("Imp", 25, 10, 7f, 1));
            _enemies.Add(new MockCombatant("Mimic", 40, 0, 6f, 1));

            var all = new List<ICombatant>();
            all.AddRange(_party);
            all.AddRange(_enemies);

            _timeline = new TimelineTurnSystem();
            _battle = new Battle();

            _battle.OnDamageDealt += e =>
            {
                string crit = e.WasCrit ? " A critical hit!" : "";
                Log($"{e.Source.Name} attacks! {e.Target.Name} takes {e.Amount} damage.{crit}");
            };

            _battle.OnHealed += e =>
                Log($"{e.Source.Name} casts Heal! {e.Target.Name} recovers {e.Amount} HP.");

            _battle.OnKO += e =>
                Log($"{e.Target.Name} is defeated!");

            _battle.OnRevived += c =>
                Log($"{c.Name} is brought back to life!");

            _battle.OnFled += c =>
                Log($"The party flees from battle!");

            _battle.OnBattleEnd += (b, state) =>
            {
                if (state == BattleState.Victory)
                {
                    Log("");
                    Log("Thou hast done well in defeating the monsters.");
                }
                else if (state == BattleState.Defeat)
                {
                    Log("");
                    Log("Thou art dead.");
                }
                commandPanel.SetActive(false);
                UpdateTurnIndicator();
                RefreshStatus();
                RefreshTimeline();
            };

            _battle.Start(all, _timeline);

            btnFight.onClick.AddListener(OnFight);
            btnHeal.onClick.AddListener(OnHeal);
            btnDefend.onClick.AddListener(OnDefend);
            btnRevive.onClick.AddListener(OnRevive);
            btnFlee.onClick.AddListener(OnFlee);
            btnBack.onClick.AddListener(OnBack);

            for (int i = 0; i < targetButtons.Length; i++)
            {
                int idx = i;
                targetButtons[i].onClick.AddListener(() => OnSelectTarget(idx));

                var trigger = targetButtons[i].gameObject.AddComponent<EventTrigger>();
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => OnTargetHover(idx, true));
                trigger.triggers.Add(enter);
                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ => OnTargetHover(idx, false));
                trigger.triggers.Add(exit);
            }

            Log("A group of monsters appeared!");
            Log("");
            RefreshStatus();
            NextTurn();
        }

        private void NextTurn()
        {
            if (!IsActive()) return;

            if (_battle.State == BattleState.Executing)
            {
                _timeline.SetActionCost(_actionCost);
                _battle.EndTurn();
            }

            if (!IsActive()) return;

            var actor = _battle.BeginNextTurn();
            if (actor == null) return;

            RefreshTimeline();

            if (actor.Team == 0)
            {
                Log($"What will {actor.Name} do?");
                _waitingForCommand = true;
                commandPanel.SetActive(true);
                ShowCommandButtons(true);
                RefreshCommandButtons();
            }
            else
            {
                commandPanel.SetActive(false);
                _waitingForCommand = false;
                EnemyAction(actor);
            }

            RefreshStatus();
            UpdateTurnIndicator();
        }

        // ── Actions ──

        private void EnemyAction(ICombatant actor)
        {
            var alive = _battle.GetAlive(0);
            if (alive.Count == 0) return;

            var target = alive[Random.Range(0, alive.Count)];
            int damage = Random.Range(4, 14);
            _battle.DealDamage(actor, target, damage, "physical");
            _actionCost = CostEnemy;
            RefreshStatus();
            NextTurn();
        }

        private void OnFight()
        {
            if (!_waitingForCommand) return;
            ShowCommandButtons(false);
            RefreshTargetButtons();
            btnBack.gameObject.SetActive(true);
        }

        private void OnSelectTarget(int index)
        {
            if (!_waitingForCommand) return;
            if (index < 0 || index >= _enemies.Count) return;
            if (!_enemies[index].IsAlive) return;

            _waitingForCommand = false;
            ClearEnemyHoverMarks();

            var actor = _battle.CurrentActor;
            int damage = Random.Range(10, 30);
            _battle.DealDamage(actor, _enemies[index], damage, "physical");
            _actionCost = CostFight;
            RefreshStatus();
            NextTurn();
        }

        private void OnHeal()
        {
            if (!_waitingForCommand) return;
            _waitingForCommand = false;
            commandPanel.SetActive(false);

            var actor = _battle.CurrentActor;
            var mc = actor as MockCombatant;
            if (mc != null && mc.MP < 5)
            {
                Log($"{actor.Name} doesn't have enough MP!");
                _actionCost = CostHeal;
                NextTurn();
                return;
            }

            MockCombatant target = null;
            int lowestHP = int.MaxValue;
            for (int i = 0; i < _party.Count; i++)
            {
                if (_party[i].IsAlive && _party[i].HP < _party[i].MaxHP && _party[i].HP < lowestHP)
                {
                    lowestHP = _party[i].HP;
                    target = _party[i];
                }
            }

            if (target == null)
            {
                Log($"{actor.Name} tries to cast Heal, but everyone is fine.");
            }
            else
            {
                if (mc != null) mc.MP -= 5;
                int amount = Random.Range(20, 40);
                _battle.Heal(actor, target, amount);
            }

            _actionCost = CostHeal;
            RefreshStatus();
            NextTurn();
        }

        private void OnDefend()
        {
            if (!_waitingForCommand) return;
            _waitingForCommand = false;
            commandPanel.SetActive(false);

            Log($"{_battle.CurrentActor.Name} defends.");
            _actionCost = CostDefend;
            NextTurn();
        }

        private void OnRevive()
        {
            if (!_waitingForCommand) return;
            _waitingForCommand = false;
            commandPanel.SetActive(false);

            var actor = _battle.CurrentActor;
            var mc = actor as MockCombatant;
            if (mc != null && mc.MP < 15)
            {
                Log($"{actor.Name} doesn't have enough MP!");
                _actionCost = CostRevive;
                NextTurn();
                return;
            }

            MockCombatant dead = null;
            for (int i = 0; i < _party.Count; i++)
                if (!_party[i].IsAlive) { dead = _party[i]; break; }

            if (dead == null)
            {
                Log($"{actor.Name} tries to cast Revive, but no one is dead.");
            }
            else
            {
                if (mc != null) mc.MP -= 15;
                _battle.Revive(dead, dead.MaxHP / 2);
            }

            _actionCost = CostRevive;
            RefreshStatus();
            NextTurn();
        }

        private void OnFlee()
        {
            if (!_waitingForCommand) return;
            _waitingForCommand = false;
            commandPanel.SetActive(false);

            if (Random.value < 0.5f)
            {
                Log("But the party failed to escape!");
                _actionCost = CostFight;
                NextTurn();
            }
            else
            {
                _battle.Flee(_battle.CurrentActor);
            }
        }

        private void OnBack()
        {
            ClearEnemyHoverMarks();
            ShowCommandButtons(true);
        }

        // ── UI ──

        private void RefreshTimeline()
        {
            var order = _battle.GetTimeline();
            var current = _battle.CurrentActor;

            for (int i = 0; i < timelineSlots.Length; i++)
            {
                if (i < order.Count)
                {
                    var c = order[i];
                    bool isCurrent = c == current;
                    string prefix = isCurrent ? "> " : "  ";
                    int tick = _timeline.GetTick(c);
                    timelineSlots[i].text = $"{prefix}{c.Name} ({tick})";
                    timelineSlots[i].color = c.Team == 0 ? GoldColor : Color.white;
                    timelineSlots[i].fontStyle = isCurrent ? FontStyles.Bold : FontStyles.Normal;
                    timelineSlots[i].gameObject.SetActive(true);
                }
                else
                {
                    timelineSlots[i].gameObject.SetActive(false);
                }
            }
        }

        private void RefreshStatus()
        {
            for (int i = 0; i < _party.Count; i++)
            {
                var c = _party[i];
                if (i < partyNameTexts.Length && partyNameTexts[i] != null)
                    partyNameTexts[i].text = c.Name;
                if (i < partyHPBars.Length && partyHPBars[i] != null)
                    partyHPBars[i].value = c.MaxHP > 0 ? (float)c.HP / c.MaxHP : 0;
                if (i < partyMPBars.Length && partyMPBars[i] != null)
                    partyMPBars[i].value = c.MaxMP > 0 ? (float)c.MP / c.MaxMP : 0;
                if (i < partyHPTexts.Length && partyHPTexts[i] != null)
                    partyHPTexts[i].text = c.IsAlive ? $"HP {c.HP}/{c.MaxHP}" : "HP 0 (dead)";
                if (i < partyMPTexts.Length && partyMPTexts[i] != null)
                    partyMPTexts[i].text = $"MP {c.MP}/{c.MaxMP}";
                if (i < partyPortraits.Length && partyPortraits[i] != null)
                    partyPortraits[i].color = c.IsAlive ? Color.white : new Color(0.2f, 0.2f, 0.2f);
            }

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (i < enemyNameTexts.Length && enemyNameTexts[i] != null)
                    enemyNameTexts[i].text = e.IsAlive ? e.Name : $"{e.Name} (dead)";
                if (i < enemyHPBars.Length && enemyHPBars[i] != null)
                    enemyHPBars[i].value = e.MaxHP > 0 ? (float)e.HP / e.MaxHP : 0;
                if (i < enemyHPTexts.Length && enemyHPTexts[i] != null)
                    enemyHPTexts[i].text = e.IsAlive ? $"HP {e.HP}/{e.MaxHP}" : "HP 0 (dead)";
                if (i < enemyPortraits.Length && enemyPortraits[i] != null)
                    enemyPortraits[i].color = e.IsAlive ? Color.white : new Color(0.2f, 0.2f, 0.2f);
            }
        }

        private void RefreshCommandButtons()
        {
            var mc = _battle.CurrentActor as MockCombatant;
            bool isPriest = mc != null && mc.Name == "Priest";

            btnHeal.interactable = isPriest && mc.MP >= 5;

            bool hasDead = false;
            for (int i = 0; i < _party.Count; i++)
                if (!_party[i].IsAlive) { hasDead = true; break; }
            btnRevive.interactable = isPriest && hasDead && mc.MP >= 15;
        }

        private void RefreshTargetButtons()
        {
            for (int i = 0; i < targetButtons.Length; i++)
            {
                if (i < _enemies.Count)
                {
                    targetButtons[i].gameObject.SetActive(true);
                    var label = targetButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null)
                        label.text = _enemies[i].IsAlive ? _enemies[i].Name : $"{_enemies[i].Name} (dead)";

                    bool alive = _enemies[i].IsAlive;
                    targetButtons[i].interactable = alive;
                    if (label != null)
                        label.color = alive ? Color.white : new Color(0.4f, 0.4f, 0.4f);
                }
                else
                {
                    targetButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void ShowCommandButtons(bool show)
        {
            btnFight.gameObject.SetActive(show);
            btnHeal.gameObject.SetActive(show);
            btnDefend.gameObject.SetActive(show);
            btnRevive.gameObject.SetActive(show);
            btnFlee.gameObject.SetActive(show);

            if (show)
            {
                for (int i = 0; i < targetButtons.Length; i++)
                    targetButtons[i].gameObject.SetActive(false);
                btnBack.gameObject.SetActive(false);
            }
        }

        private void UpdateTurnIndicator()
        {
            for (int i = 0; i < partySelectMarks.Length; i++)
                if (partySelectMarks[i] != null) partySelectMarks[i].SetActive(false);
            for (int i = 0; i < enemySelectMarks.Length; i++)
                if (enemySelectMarks[i] != null) enemySelectMarks[i].SetActive(false);

            var current = _battle.CurrentActor;
            if (current == null) return;

            for (int i = 0; i < _party.Count; i++)
            {
                if (_party[i] == current && i < partySelectMarks.Length && partySelectMarks[i] != null)
                {
                    partySelectMarks[i].SetActive(true);
                    return;
                }
            }
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i] == current && i < enemySelectMarks.Length && enemySelectMarks[i] != null)
                {
                    enemySelectMarks[i].SetActive(true);
                    return;
                }
            }
        }

        private void OnTargetHover(int index, bool hovering)
        {
            if (index < enemySelectMarks.Length && enemySelectMarks[index] != null)
                enemySelectMarks[index].SetActive(hovering);
        }

        private void ClearEnemyHoverMarks()
        {
            for (int i = 0; i < enemySelectMarks.Length; i++)
                if (enemySelectMarks[i] != null) enemySelectMarks[i].SetActive(false);
        }

        private void Log(string message)
        {
            if (messageContent == null) return;

            var go = new GameObject($"Msg_{_logCount++}");
            go.transform.SetParent(messageContent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.fontSize = 22;
            tmp.color = Color.white;
            tmp.richText = true;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = string.IsNullOrEmpty(message) ? 10 : 28;

            Canvas.ForceUpdateCanvases();
            if (messageScrollRect != null)
                messageScrollRect.verticalNormalizedPosition = 0f;
        }

        private bool IsActive()
        {
            return _battle.State == BattleState.WaitingForCommands ||
                   _battle.State == BattleState.Executing;
        }

        public void LoadMainMenu()
        {
            if (!string.IsNullOrEmpty(hubSceneName))
                UnityEngine.SceneManagement.SceneManager.LoadScene(hubSceneName);
        }
    }
}
