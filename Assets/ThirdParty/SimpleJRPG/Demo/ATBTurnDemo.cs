using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SimpleJRPG;

namespace SimpleJRPG.Demo
{
    public class ATBTurnDemo : MonoBehaviour
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
        public Slider[] enemyATBBars;

        [Header("Party Portraits")]
        public Image[] partyPortraits;
        public TextMeshProUGUI[] partyNameTexts;
        public Slider[] partyHPBars;
        public Slider[] partyMPBars;
        public TextMeshProUGUI[] partyHPTexts;
        public TextMeshProUGUI[] partyMPTexts;
        public Slider[] partyATBBars;

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

        [Header("ATB Settings")]
        public float timeScale = 10f;
        public Button btnMode;

        // ── State ──

        private Battle _battle;
        private ATBTurnSystem _atb;
        private List<MockCombatant> _party = new List<MockCombatant>();
        private List<MockCombatant> _enemies = new List<MockCombatant>();
        private int _logCount;
        private bool _waitMode = true;

        // Active mode: track pending party member separately from Battle turn system
        private ICombatant _pendingActor;
        private float[] _savedPartyGauges;
        private float[] _savedEnemyGauges;

        void Start()
        {
            _party.Add(new MockCombatant("Hero", 120, 30, 10f, 0));
            _party.Add(new MockCombatant("Warrior", 100, 0, 8f, 0));
            _party.Add(new MockCombatant("Mage", 60, 80, 12f, 0));
            _party.Add(new MockCombatant("Priest", 80, 60, 9f, 0));

            _enemies.Add(new MockCombatant("Blob", 30, 0, 5f, 1));
            _enemies.Add(new MockCombatant("Imp", 25, 10, 7f, 1));
            _enemies.Add(new MockCombatant("Mimic", 40, 0, 6f, 1));

            _savedPartyGauges = new float[_party.Count];
            _savedEnemyGauges = new float[_enemies.Count];

            var all = new List<ICombatant>();
            all.AddRange(_party);
            all.AddRange(_enemies);

            _atb = new ATBTurnSystem();
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
                _pendingActor = null;
                commandPanel.SetActive(false);
                UpdateTurnIndicator();
                RefreshStatus();
            };

            _battle.Start(all, _atb);

            btnFight.onClick.AddListener(OnFight);
            btnHeal.onClick.AddListener(OnHeal);
            btnDefend.onClick.AddListener(OnDefend);
            btnRevive.onClick.AddListener(OnRevive);
            btnFlee.onClick.AddListener(OnFlee);
            btnBack.onClick.AddListener(OnBack);
            btnMode.onClick.AddListener(OnToggleMode);

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

            commandPanel.SetActive(false);

            Log("A group of monsters appeared!");
            Log("");
            RefreshStatus();
        }

        void Update()
        {
            if (!IsActive()) return;

            // Tick ATB — pause only in Wait mode while player is deciding
            bool shouldTick = !_waitMode || _pendingActor == null;
            if (shouldTick)
                _atb.Tick(Time.deltaTime * timeScale);

            RefreshATBBars();

            // Process ready enemies (even while player is deciding in Active mode)
            ProcessReadyEnemies();

            if (!IsActive()) return;

            // Check for ready party members
            if (_pendingActor == null)
            {
                for (int i = 0; i < _party.Count; i++)
                {
                    if (_party[i].IsAlive && _atb.IsReady(_party[i]))
                    {
                        _pendingActor = _party[i];
                        Log($"{_pendingActor.Name} is ready!");
                        commandPanel.SetActive(true);
                        ShowCommandButtons(true);
                        RefreshCommandButtons();
                        UpdateTurnIndicator();
                        break;
                    }
                }
            }
        }

        // ── Turn Helpers ──

        private void ProcessReadyEnemies()
        {
            // Temporarily zero all party gauges so GetNextActor only returns enemies
            for (int i = 0; i < _party.Count; i++)
            {
                _savedPartyGauges[i] = _atb.GetGauge(_party[i]);
                _atb.SetGauge(_party[i], 0);
            }

            while (_atb.HasReadyActor() && IsActive())
            {
                var actor = _battle.BeginNextTurn();
                if (actor == null) break;

                EnemyAction(actor);
                _battle.EndTurn();
                RefreshStatus();
            }

            // Restore party gauges
            for (int i = 0; i < _party.Count; i++)
                _atb.SetGauge(_party[i], _savedPartyGauges[i]);

            // If pending actor died from enemy attack, clear them
            if (_pendingActor != null && !_pendingActor.IsAlive)
            {
                _pendingActor = null;
                commandPanel.SetActive(false);
                UpdateTurnIndicator();
            }
        }

        private ICombatant BeginPendingTurn()
        {
            // Zero all other gauges so GetNextActor returns _pendingActor
            for (int i = 0; i < _party.Count; i++)
            {
                _savedPartyGauges[i] = _atb.GetGauge(_party[i]);
                if (_party[i] != _pendingActor)
                    _atb.SetGauge(_party[i], 0);
            }
            for (int i = 0; i < _enemies.Count; i++)
            {
                _savedEnemyGauges[i] = _atb.GetGauge(_enemies[i]);
                _atb.SetGauge(_enemies[i], 0);
            }

            return _battle.BeginNextTurn();
        }

        private void FinishAction()
        {
            if (_battle.State == BattleState.Executing)
                _battle.EndTurn();

            // Restore other combatant gauges (pending actor's gauge was reset by EndTurn)
            for (int i = 0; i < _party.Count; i++)
            {
                if (_party[i] != _pendingActor)
                    _atb.SetGauge(_party[i], _savedPartyGauges[i]);
            }
            for (int i = 0; i < _enemies.Count; i++)
                _atb.SetGauge(_enemies[i], _savedEnemyGauges[i]);

            _pendingActor = null;
            commandPanel.SetActive(false);
            ClearEnemyHoverMarks();
            UpdateTurnIndicator();
            RefreshStatus();
        }

        // ── Actions ──

        private void EnemyAction(ICombatant actor)
        {
            var alive = _battle.GetAlive(0);
            if (alive.Count == 0) return;

            var target = alive[Random.Range(0, alive.Count)];
            int damage = Random.Range(4, 14);
            _battle.DealDamage(actor, target, damage, "physical");
            RefreshStatus();
        }

        private void OnFight()
        {
            if (_pendingActor == null) return;
            ShowCommandButtons(false);
            RefreshTargetButtons();
            btnBack.gameObject.SetActive(true);
        }

        private void OnSelectTarget(int index)
        {
            if (_pendingActor == null) return;
            if (index < 0 || index >= _enemies.Count) return;
            if (!_enemies[index].IsAlive) return;

            var actor = BeginPendingTurn();
            int damage = Random.Range(10, 30);
            _battle.DealDamage(actor, _enemies[index], damage, "physical");
            FinishAction();
        }

        private void OnHeal()
        {
            if (_pendingActor == null) return;

            var mc = _pendingActor as MockCombatant;
            if (mc != null && mc.MP < 5)
            {
                Log($"{_pendingActor.Name} doesn't have enough MP!");
                BeginPendingTurn();
                FinishAction();
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

            var actor = BeginPendingTurn();
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

            FinishAction();
        }

        private void OnDefend()
        {
            if (_pendingActor == null) return;
            Log($"{_pendingActor.Name} defends.");
            BeginPendingTurn();
            FinishAction();
        }

        private void OnRevive()
        {
            if (_pendingActor == null) return;

            var mc = _pendingActor as MockCombatant;
            if (mc != null && mc.MP < 15)
            {
                Log($"{_pendingActor.Name} doesn't have enough MP!");
                BeginPendingTurn();
                FinishAction();
                return;
            }

            MockCombatant dead = null;
            for (int i = 0; i < _party.Count; i++)
                if (!_party[i].IsAlive) { dead = _party[i]; break; }

            var actor = BeginPendingTurn();
            if (dead == null)
            {
                Log($"{actor.Name} tries to cast Revive, but no one is dead.");
            }
            else
            {
                if (mc != null) mc.MP -= 15;
                _battle.Revive(dead, dead.MaxHP / 2);
            }

            FinishAction();
        }

        private void OnFlee()
        {
            if (_pendingActor == null) return;

            if (Random.value < 0.5f)
            {
                Log("But the party failed to escape!");
                BeginPendingTurn();
                FinishAction();
            }
            else
            {
                var actor = BeginPendingTurn();
                _battle.Flee(actor);
                FinishAction();
            }
        }

        private void OnBack()
        {
            ClearEnemyHoverMarks();
            ShowCommandButtons(true);
        }

        private void OnToggleMode()
        {
            _waitMode = !_waitMode;
            var label = btnMode.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = _waitMode ? "MODE: WAIT" : "MODE: ACTIVE";
        }

        // ── UI ──

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

        private void RefreshATBBars()
        {
            for (int i = 0; i < _party.Count; i++)
            {
                if (i < partyATBBars.Length && partyATBBars[i] != null)
                    partyATBBars[i].value = _party[i].IsAlive ? _atb.GetGauge(_party[i]) / 100f : 0;
            }
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (i < enemyATBBars.Length && enemyATBBars[i] != null)
                    enemyATBBars[i].value = _enemies[i].IsAlive ? _atb.GetGauge(_enemies[i]) / 100f : 0;
            }
        }

        private void RefreshCommandButtons()
        {
            var mc = _pendingActor as MockCombatant;
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

            var current = _pendingActor ?? _battle.CurrentActor;
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
