using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SimpleJRPG;

namespace SimpleJRPG.Demo
{
    public class PressTurnDemo : MonoBehaviour
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
        public Button btnMagic;
        public Button btnHeal;
        public Button btnPass;
        public Button btnFlee;
        public GameObject commandPanel;

        [Header("Actor Selection")]
        public Button[] actorButtons;

        [Header("Target Selection")]
        public Button[] targetButtons;
        public Button btnBack;

        [Header("Turn Indicators")]
        public GameObject[] partySelectMarks;
        public GameObject[] enemySelectMarks;

        [Header("Press Turn")]
        public TextMeshProUGUI phaseLabel;
        public TextMeshProUGUI[] pressIcons;

        // ── State ──

        private enum InputMode { None, ActorSelect, Command, FightTarget, MagicTarget }

        private Battle _battle;
        private PressTurnSystem _pts;
        private List<MockCombatant> _party = new List<MockCombatant>();
        private List<MockCombatant> _enemies = new List<MockCombatant>();
        private int _logCount;
        private InputMode _mode = InputMode.None;
        private ICombatant _selectedActor;

        // Demo-only element/weakness data
        private Dictionary<string, string> _castElement;
        private Dictionary<string, string> _weakness;

        void Start()
        {
            // Party (team 0)
            _party.Add(new MockCombatant("Hero", 120, 40, 10f, 0));
            _party.Add(new MockCombatant("Warrior", 140, 0, 7f, 0));
            _party.Add(new MockCombatant("Mage", 80, 80, 12f, 0));
            _party.Add(new MockCombatant("Priest", 90, 60, 9f, 0));

            // Enemies (team 1)
            _enemies.Add(new MockCombatant("Blob", 30, 0, 5f, 1));
            _enemies.Add(new MockCombatant("Imp", 25, 10, 7f, 1));
            _enemies.Add(new MockCombatant("Mimic", 40, 0, 6f, 1));

            // Element/weakness tables
            _castElement = new Dictionary<string, string>
            {
                { "Hero", "fire" },
                { "Mage", "ice" },
                { "Priest", "lightning" },
                { "Warrior", "" }
            };

            _weakness = new Dictionary<string, string>
            {
                { "Blob", "fire" },
                { "Imp", "ice" },
                { "Mimic", "lightning" },
                { "Hero", "ice" },
                { "Mage", "lightning" },
                { "Priest", "fire" },
                { "Warrior", "ice" }
            };

            var all = new List<ICombatant>();
            all.AddRange(_party);
            all.AddRange(_enemies);

            _pts = new PressTurnSystem(4);
            _battle = new Battle();

            // Events
            _battle.OnDamageDealt += e =>
            {
                string crit = e.WasCrit ? " Critical hit!" : "";
                Log($"{e.Source.Name} attacks {e.Target.Name} for {e.Amount} damage.{crit}");
            };

            _battle.OnHealed += e =>
                Log($"{e.Source.Name} heals {e.Target.Name} for {e.Amount} HP.");

            _battle.OnKO += e =>
                Log($"{e.Target.Name} has been defeated!");

            _battle.OnRevived += c =>
                Log($"{c.Name} has been revived!");

            _battle.OnFled += c =>
                Log($"The party fled from battle!");

            _battle.OnBattleEnd += (b, state) =>
            {
                if (state == BattleState.Victory)
                {
                    Log("");
                    Log("All enemies have been vanquished.");
                }
                else if (state == BattleState.Defeat)
                {
                    Log("");
                    Log("The party has been wiped out...");
                }
                SetMode(InputMode.None);
                UpdatePressIcons();
                RefreshStatus();
            };

            _battle.Start(all, _pts);

            // Button listeners
            btnFight.onClick.AddListener(OnFight);
            btnMagic.onClick.AddListener(OnMagic);
            btnHeal.onClick.AddListener(OnHealCmd);
            btnPass.onClick.AddListener(OnPass);
            btnFlee.onClick.AddListener(OnFlee);
            btnBack.onClick.AddListener(OnBack);

            for (int i = 0; i < actorButtons.Length; i++)
            {
                int idx = i;
                actorButtons[i].onClick.AddListener(() => OnSelectActor(idx));
            }

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

            Log("Enemies appeared!");
            Log("");
            RefreshStatus();
            StartPlayerPhase();
        }

        // ── Phase Management ──

        private void StartPlayerPhase()
        {
            if (!IsActive()) return;
            Log("<color=#FFD84D>--- PLAYER PHASE ---</color>");
            UpdatePressIcons();
            SetMode(InputMode.ActorSelect);
        }

        private void StartEnemyPhase()
        {
            if (!IsActive()) return;
            Log("<color=#FF4444>--- ENEMY PHASE ---</color>");
            UpdatePressIcons();
            SetMode(InputMode.None);

            while (_pts.HasActionsRemaining && _pts.ActiveTeam == 1 && IsActive())
            {
                var alive = _battle.GetAlive(1);
                if (alive.Count == 0) break;

                var enemy = alive[Random.Range(0, alive.Count)];
                _pts.SelectActor(enemy);
                _battle.BeginNextTurn();
                EnemyAction(enemy);
                _battle.EndTurn();
                UpdatePressIcons();

                if (!IsActive()) return;
            }

            if (IsActive() && _pts.ActiveTeam == 0)
                StartPlayerPhase();
        }

        private void AfterAction()
        {
            RefreshStatus();
            UpdatePressIcons();

            if (!IsActive()) return;

            if (_pts.ActiveTeam == 0)
                SetMode(InputMode.ActorSelect);
            else
                StartEnemyPhase();
        }

        // ── Actor Selection ──

        private void OnSelectActor(int index)
        {
            if (_mode != InputMode.ActorSelect) return;
            if (index < 0 || index >= _party.Count) return;
            if (!_party[index].IsAlive) return;

            _selectedActor = _party[index];
            UpdateTurnIndicator();
            SetMode(InputMode.Command);
        }

        // ── Commands ──

        private void OnFight()
        {
            if (_mode != InputMode.Command) return;
            SetMode(InputMode.FightTarget);
        }

        private void OnMagic()
        {
            if (_mode != InputMode.Command) return;
            SetMode(InputMode.MagicTarget);
        }

        private void OnHealCmd()
        {
            if (_mode != InputMode.Command) return;

            var mc = _selectedActor as MockCombatant;
            if (mc != null && mc.MP < 5)
            {
                Log($"{_selectedActor.Name} doesn't have enough MP!");
                return;
            }

            _pts.SelectActor(_selectedActor);
            _battle.BeginNextTurn();

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
                Log($"{_selectedActor.Name} tries to heal, but everyone is fine.");
            }
            else
            {
                if (mc != null) mc.MP -= 5;
                int amount = Random.Range(25, 45);
                _battle.Heal(_selectedActor, target, amount);
            }

            _pts.ConsumeAction();
            _battle.EndTurn();
            AfterAction();
        }

        private void OnPass()
        {
            if (_mode != InputMode.Command) return;

            _pts.SelectActor(_selectedActor);
            _battle.BeginNextTurn();

            Log($"{_selectedActor.Name} passes.");
            _pts.ConvertAction();
            _battle.EndTurn();
            AfterAction();
        }

        private void OnFlee()
        {
            if (_mode != InputMode.Command) return;

            _pts.SelectActor(_selectedActor);
            _battle.BeginNextTurn();

            if (Random.value < 0.4f)
            {
                _battle.Flee(_selectedActor);
            }
            else
            {
                Log("Failed to escape!");
                _pts.ConsumeAllActions();
                _battle.EndTurn();
                AfterAction();
            }
        }

        private void OnBack()
        {
            if (_mode == InputMode.FightTarget || _mode == InputMode.MagicTarget)
            {
                ClearEnemyHoverMarks();
                SetMode(InputMode.Command);
            }
            else if (_mode == InputMode.Command)
            {
                _selectedActor = null;
                ClearAllMarks();
                SetMode(InputMode.ActorSelect);
            }
        }

        // ── Target Selection ──

        private void OnSelectTarget(int index)
        {
            if (_mode != InputMode.FightTarget && _mode != InputMode.MagicTarget) return;
            if (index < 0 || index >= _enemies.Count) return;
            if (!_enemies[index].IsAlive) return;

            ClearEnemyHoverMarks();

            bool isMagic = _mode == InputMode.MagicTarget;

            _pts.SelectActor(_selectedActor);
            _battle.BeginNextTurn();

            var target = _enemies[index];

            if (!isMagic)
            {
                // Physical: 20% crit, 10% miss
                float roll = Random.value;
                if (roll < 0.10f)
                {
                    Log($"{_selectedActor.Name} attacks {target.Name}... but misses!");
                    _pts.ConsumeActions(2);
                }
                else if (roll < 0.30f)
                {
                    int damage = Random.Range(18, 35);
                    _battle.DealDamage(_selectedActor, target, damage, "physical", "", true);
                    Log("Press turn gained!");
                    _pts.ConvertAction();
                }
                else
                {
                    int damage = Random.Range(12, 25);
                    _battle.DealDamage(_selectedActor, target, damage, "physical");
                    _pts.ConsumeAction();
                }
            }
            else
            {
                var mc = _selectedActor as MockCombatant;
                if (mc != null) mc.MP -= 8;

                string element = "";
                _castElement.TryGetValue(_selectedActor.Name, out element);

                string targetWeak = "";
                _weakness.TryGetValue(target.Name, out targetWeak);

                int damage = Random.Range(15, 30);
                bool hitWeakness = !string.IsNullOrEmpty(element) && element == targetWeak;

                _battle.DealDamage(_selectedActor, target, damage, "magic", element);

                if (hitWeakness)
                {
                    Log("<color=#55FF55>Weak!</color> Press turn gained!");
                    _pts.ConvertAction();
                }
                else
                {
                    _pts.ConsumeAction();
                }
            }

            _battle.EndTurn();
            AfterAction();
        }

        // ── Enemy AI ──

        private void EnemyAction(ICombatant actor)
        {
            var aliveParty = _battle.GetAlive(0);
            if (aliveParty.Count == 0) return;

            var target = aliveParty[Random.Range(0, aliveParty.Count)];
            bool useElemental = Random.value < 0.30f;

            if (useElemental)
            {
                string[] elements = { "fire", "ice", "lightning" };
                string element = elements[Random.Range(0, elements.Length)];

                string targetWeak = "";
                _weakness.TryGetValue(target.Name, out targetWeak);

                int damage = Random.Range(10, 22);
                bool hitWeakness = element == targetWeak;

                _battle.DealDamage(actor, target, damage, "magic", element);

                if (hitWeakness)
                {
                    Log("<color=#FF5555>Weak!</color> Enemy gains a press turn!");
                    _pts.ConvertAction();
                }
                else
                {
                    _pts.ConsumeAction();
                }
            }
            else
            {
                float roll = Random.value;
                if (roll < 0.10f)
                {
                    Log($"{actor.Name} attacks {target.Name}... but misses!");
                    _pts.ConsumeActions(2);
                }
                else if (roll < 0.30f)
                {
                    int damage = Random.Range(8, 18);
                    _battle.DealDamage(actor, target, damage, "physical", "", true);
                    _pts.ConvertAction();
                }
                else
                {
                    int damage = Random.Range(6, 15);
                    _battle.DealDamage(actor, target, damage, "physical");
                    _pts.ConsumeAction();
                }
            }

            RefreshStatus();
        }

        // ── UI Mode ──

        private void SetMode(InputMode mode)
        {
            _mode = mode;

            // Hide everything first
            for (int i = 0; i < actorButtons.Length; i++)
                actorButtons[i].gameObject.SetActive(false);
            btnFight.gameObject.SetActive(false);
            btnMagic.gameObject.SetActive(false);
            btnHeal.gameObject.SetActive(false);
            btnPass.gameObject.SetActive(false);
            btnFlee.gameObject.SetActive(false);
            for (int i = 0; i < targetButtons.Length; i++)
                targetButtons[i].gameObject.SetActive(false);
            btnBack.gameObject.SetActive(false);

            switch (mode)
            {
                case InputMode.ActorSelect:
                    commandPanel.SetActive(true);
                    for (int i = 0; i < actorButtons.Length && i < _party.Count; i++)
                    {
                        actorButtons[i].gameObject.SetActive(true);
                        var label = actorButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (label != null)
                            label.text = _party[i].Name;
                        actorButtons[i].interactable = _party[i].IsAlive;
                        if (label != null)
                            label.color = _party[i].IsAlive ? Color.white : new Color(0.4f, 0.4f, 0.4f);
                    }
                    _selectedActor = null;
                    ClearAllMarks();
                    break;

                case InputMode.Command:
                    commandPanel.SetActive(true);
                    btnFight.gameObject.SetActive(true);
                    btnMagic.gameObject.SetActive(true);
                    btnHeal.gameObject.SetActive(true);
                    btnPass.gameObject.SetActive(true);
                    btnFlee.gameObject.SetActive(true);
                    btnBack.gameObject.SetActive(true);
                    RefreshCommandButtons();
                    break;

                case InputMode.FightTarget:
                    commandPanel.SetActive(true);
                    RefreshTargetButtons(false);
                    btnBack.gameObject.SetActive(true);
                    break;

                case InputMode.MagicTarget:
                    commandPanel.SetActive(true);
                    RefreshTargetButtons(true);
                    btnBack.gameObject.SetActive(true);
                    break;

                case InputMode.None:
                    commandPanel.SetActive(false);
                    break;
            }
        }

        private void RefreshCommandButtons()
        {
            var mc = _selectedActor as MockCombatant;
            bool isCaster = mc != null && (mc.Name == "Hero" || mc.Name == "Mage");
            btnMagic.interactable = isCaster && mc.MP >= 8;
            btnHeal.interactable = mc != null && mc.Name == "Priest" && mc.MP >= 5;
        }

        private void RefreshTargetButtons(bool showWeakness)
        {
            for (int i = 0; i < targetButtons.Length; i++)
            {
                if (i < _enemies.Count)
                {
                    targetButtons[i].gameObject.SetActive(true);
                    var label = targetButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null)
                    {
                        if (showWeakness && _enemies[i].IsAlive)
                        {
                            string weak = "";
                            _weakness.TryGetValue(_enemies[i].Name, out weak);
                            label.text = $"{_enemies[i].Name} <size=16>(weak: {weak})</size>";
                        }
                        else
                        {
                            label.text = _enemies[i].IsAlive ? _enemies[i].Name : $"{_enemies[i].Name} (dead)";
                        }
                        label.color = _enemies[i].IsAlive ? Color.white : new Color(0.4f, 0.4f, 0.4f);
                    }
                    targetButtons[i].interactable = _enemies[i].IsAlive;
                }
                else
                {
                    targetButtons[i].gameObject.SetActive(false);
                }
            }
        }

        // ── Press Turn Icons ──

        private void UpdatePressIcons()
        {
            if (phaseLabel != null)
            {
                if (_pts.ActiveTeam == 0)
                {
                    phaseLabel.text = "PLAYER PHASE";
                    phaseLabel.color = new Color(1f, 0.85f, 0.3f);
                }
                else
                {
                    phaseLabel.text = "ENEMY PHASE";
                    phaseLabel.color = new Color(1f, 0.27f, 0.27f);
                }
            }

            int full = _pts.FullPoints;
            int half = _pts.HalfPoints;

            for (int i = 0; i < pressIcons.Length; i++)
            {
                if (i < full)
                {
                    pressIcons[i].gameObject.SetActive(true);
                    pressIcons[i].text = "[+]";
                    pressIcons[i].color = Color.white;
                }
                else if (i < full + half)
                {
                    pressIcons[i].gameObject.SetActive(true);
                    pressIcons[i].text = "[-]";
                    pressIcons[i].color = new Color(0.6f, 0.6f, 0.6f);
                }
                else
                {
                    pressIcons[i].gameObject.SetActive(false);
                }
            }
        }

        // ── Status Refresh ──

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

        // ── Turn Indicators ──

        private void UpdateTurnIndicator()
        {
            ClearAllMarks();

            var current = _selectedActor ?? _battle.CurrentActor;
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

        private void ClearAllMarks()
        {
            for (int i = 0; i < partySelectMarks.Length; i++)
                if (partySelectMarks[i] != null) partySelectMarks[i].SetActive(false);
            for (int i = 0; i < enemySelectMarks.Length; i++)
                if (enemySelectMarks[i] != null) enemySelectMarks[i].SetActive(false);
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
