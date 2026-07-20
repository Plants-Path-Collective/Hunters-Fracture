using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SimpleJRPG;

namespace SimpleJRPG.Demo
{
    public class ActionPointDemo : MonoBehaviour
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
        public TextMeshProUGUI[] enemyAPTexts;

        [Header("Party Portraits")]
        public Image[] partyPortraits;
        public TextMeshProUGUI[] partyNameTexts;
        public Slider[] partyHPBars;
        public Slider[] partyMPBars;
        public TextMeshProUGUI[] partyHPTexts;
        public TextMeshProUGUI[] partyMPTexts;
        public TextMeshProUGUI[] partyAPTexts;

        [Header("Commands")]
        public Button btnSpend, btnSave;
        public Button btnFight, btnMagic, btnHeal, btnFlee;
        public GameObject commandPanel;

        [Header("Target Selection")]
        public Button[] targetButtons;
        public Button btnBack;

        [Header("Turn Indicators")]
        public GameObject[] partySelectMarks;
        public GameObject[] enemySelectMarks;

        // ── State ──

        private Battle _battle;
        private List<MockCombatant> _party = new List<MockCombatant>();
        private List<MockCombatant> _enemies = new List<MockCombatant>();
        private int _logCount;
        private bool _waitingForCommand;
        private ActionPointTurnSystem _apts;
        private string _pendingAction; // "fight" or "magic" for target select

        void Start()
        {
            _party.Add(new MockCombatant("Hero", 120, 40, 10f, 0));
            _party.Add(new MockCombatant("Warrior", 140, 0, 7f, 0));
            _party.Add(new MockCombatant("Mage", 80, 80, 12f, 0));
            _party.Add(new MockCombatant("Priest", 90, 60, 9f, 0));

            _enemies.Add(new MockCombatant("Blob", 30, 0, 5f, 1));
            _enemies.Add(new MockCombatant("Imp", 25, 10, 7f, 1));
            _enemies.Add(new MockCombatant("Mimic", 40, 0, 6f, 1));

            var all = new List<ICombatant>();
            all.AddRange(_party);
            all.AddRange(_enemies);

            _apts = new ActionPointTurnSystem();
            _battle = new Battle();

            _battle.OnDamageDealt += e =>
            {
                string crit = e.WasCrit ? " A critical hit!" : "";
                Log($"{e.Source.Name} attacks {e.Target.Name} for {e.Amount} damage!{crit}");
            };

            _battle.OnHealed += e =>
                Log($"{e.Source.Name} casts Heal! {e.Target.Name} recovers {e.Amount} HP.");

            _battle.OnKO += e =>
                Log($"{e.Target.Name} is defeated!");

            _battle.OnFled += c =>
                Log($"The party flees from battle!");

            _battle.OnBattleEnd += (b, state) =>
            {
                if (state == BattleState.Victory)
                {
                    Log("");
                    Log("Victory! The enemies have been vanquished.");
                }
                else if (state == BattleState.Defeat)
                {
                    Log("");
                    Log("The party has fallen...");
                }
                commandPanel.SetActive(false);
                UpdateTurnIndicator();
                RefreshStatus();
            };

            _battle.Start(all, _apts);

            btnSpend.onClick.AddListener(OnSpend);
            btnSave.onClick.AddListener(OnSave);
            btnFight.onClick.AddListener(OnFight);
            btnMagic.onClick.AddListener(OnMagic);
            btnHeal.onClick.AddListener(OnHeal);
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

            Log("<color=#FFD94A>── ACTION POINT BATTLE ──</color>");
            Log("Everyone starts with <color=#4DCC5A>1 AP</color>. AP recovers +1 each round.");
            Log("");
            Log("<color=#FFD94A>SPEND</color> = Use 1 AP to gain an extra action this turn.");
            Log("  You can SPEND multiple times, then pick an action.");
            Log("  Going negative means you <color=#E64D4D>skip turns</color> until AP recovers to 0.");
            Log("<color=#FFD94A>SAVE</color> = Bank +1 AP and skip your turn. Max 4 AP.");
            Log("");
            Log("A group of enemies appeared!");
            Log("");
            RefreshStatus();
            NextTurn();
        }

        // ── Turn Flow ──

        private void NextTurn()
        {
            if (!IsActive()) return;

            if (_battle.State == BattleState.Executing)
                _battle.EndTurn();

            if (!IsActive()) return;

            // Snapshot negative-AP party members before getting next actor
            var negativeAPParty = new HashSet<MockCombatant>();
            foreach (var p in _party)
            {
                if (p.IsAlive && _apts.GetAP(p) < 0)
                    negativeAPParty.Add(p);
            }

            var actor = _battle.BeginNextTurn();
            if (actor == null) return;

            // Log skip messages for negative-AP party members who were passed
            foreach (var p in negativeAPParty)
            {
                if (p != actor && p.IsAlive && _apts.GetAP(p) < 0)
                    Log($"<color=#E64D4D>{p.Name} is skipped — negative AP ({_apts.GetAP(p)}), recovering...</color>");
            }

            Log($"<color=#888888>[{actor.Name}'s turn — AP: {_apts.GetAP(actor)}]</color>");

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

        // ── Enemy AI ──

        private void EnemyAction(ICombatant actor)
        {
            var alive = _battle.GetAlive(0);
            if (alive.Count == 0) return;

            // 25% chance to spend AP for extra action if AP >= 1
            if (_apts.GetAP(actor) >= 1 && Random.value < 0.25f)
            {
                int before = _apts.GetAP(actor);
                _apts.SpendAP();
                Log($"{actor.Name} spends AP ({before} -> {_apts.GetAP(actor)}) for an extra action!");
            }

            // 15% chance to save AP instead of attacking
            if (_apts.GetAP(actor) < _apts.MaxAP && Random.value < 0.15f)
            {
                int before = _apts.GetAP(actor);
                _apts.SaveAP();
                Log($"{actor.Name} saves AP ({before} -> {_apts.GetAP(actor)}) and skips their turn.");
                RefreshStatus();
                NextTurn();
                return;
            }

            // Attack random alive party target
            var target = alive[Random.Range(0, alive.Count)];
            int damage = Random.Range(8, 19);
            _battle.DealDamage(actor, target, damage, "physical");
            RefreshStatus();
            NextTurn();
        }

        // ── Player Commands ──

        private void OnSpend()
        {
            if (!_waitingForCommand) return;

            var actor = _battle.CurrentActor;
            int before = _apts.GetAP(actor);
            _apts.SpendAP();
            int after = _apts.GetAP(actor);
            Log($"{actor.Name} spends AP! ({before} -> {after}) — pick an action for the extra turn.");

            RefreshStatus();
            RefreshCommandButtons();
        }

        private void OnSave()
        {
            if (!_waitingForCommand) return;
            _waitingForCommand = false;

            var actor = _battle.CurrentActor;
            int before = _apts.GetAP(actor);
            _apts.SaveAP();
            int after = _apts.GetAP(actor);
            Log($"{actor.Name} saves AP ({before} -> {after}) and skips their turn.");

            RefreshStatus();
            NextTurn();
        }

        private void OnFight()
        {
            if (!_waitingForCommand) return;
            _pendingAction = "fight";
            ShowCommandButtons(false);
            RefreshTargetButtons();
            btnBack.gameObject.SetActive(true);
        }

        private void OnMagic()
        {
            if (!_waitingForCommand) return;
            _pendingAction = "magic";
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
            var mc = actor as MockCombatant;

            if (_pendingAction == "magic")
            {
                if (mc != null) mc.MP -= 8;
                int damage = Random.Range(18, 33);
                _battle.DealDamage(actor, _enemies[index], damage, "magic");
            }
            else
            {
                int damage = Random.Range(12, 26);
                _battle.DealDamage(actor, _enemies[index], damage, "physical");
            }

            RefreshStatus();
            NextTurn();
        }

        private void OnHeal()
        {
            if (!_waitingForCommand) return;
            _waitingForCommand = false;

            var actor = _battle.CurrentActor;
            var mc = actor as MockCombatant;
            if (mc != null && mc.MP < 5)
            {
                Log($"{actor.Name} doesn't have enough MP!");
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
                int amount = Random.Range(25, 46);
                _battle.Heal(actor, target, amount);
            }

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
            RefreshCommandButtons();
        }

        // ── UI Show/Hide ──

        private void ShowCommandButtons(bool show)
        {
            btnSpend.gameObject.SetActive(show);
            btnSave.gameObject.SetActive(show);
            btnFight.gameObject.SetActive(show);
            btnMagic.gameObject.SetActive(show);
            btnHeal.gameObject.SetActive(show);
            btnFlee.gameObject.SetActive(show);

            if (show)
            {
                for (int i = 0; i < targetButtons.Length; i++)
                    targetButtons[i].gameObject.SetActive(false);
                btnBack.gameObject.SetActive(false);
            }
        }

        private void RefreshCommandButtons()
        {
            var actor = _battle.CurrentActor;
            var mc = actor as MockCombatant;

            // SPEND: disabled if at min AP
            btnSpend.interactable = _apts.GetAP(actor) > _apts.MinAP;

            // SAVE: always enabled
            btnSave.interactable = true;

            // MAGIC: Hero/Mage only, MP >= 8
            bool canMagic = mc != null && (mc.Name == "Hero" || mc.Name == "Mage") && mc.MP >= 8;
            btnMagic.interactable = canMagic;

            // HEAL: Priest only, MP >= 5
            bool canHeal = mc != null && mc.Name == "Priest" && mc.MP >= 5;
            btnHeal.interactable = canHeal;
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

        // ── UI Refresh ──

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
                if (i < partyAPTexts.Length && partyAPTexts[i] != null)
                {
                    int ap = _apts.GetAP(c);
                    partyAPTexts[i].text = $"AP: {ap}";
                    partyAPTexts[i].color = ap > 0 ? new Color(0.3f, 0.8f, 0.35f) :
                                            ap < 0 ? new Color(0.9f, 0.3f, 0.3f) : Color.white;
                }

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
                if (i < enemyAPTexts.Length && enemyAPTexts[i] != null)
                {
                    int ap = _apts.GetAP(e);
                    enemyAPTexts[i].text = $"AP: {ap}";
                    enemyAPTexts[i].color = ap > 0 ? new Color(0.3f, 0.8f, 0.35f) :
                                            ap < 0 ? new Color(0.9f, 0.3f, 0.3f) : Color.white;
                }

                if (i < enemyPortraits.Length && enemyPortraits[i] != null)
                    enemyPortraits[i].color = e.IsAlive ? Color.white : new Color(0.2f, 0.2f, 0.2f);
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
