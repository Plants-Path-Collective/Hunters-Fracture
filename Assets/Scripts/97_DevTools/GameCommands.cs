#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core;
using Core.ChallengesSystem;
using Core.CombatSystem;
using Core.CombatSystem.Units;
using Dungeon;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting;

namespace DevTools
{
    /// <summary>
    /// Game-specific console commands. Replaces the earlier GameCommands.cs (delete that one).
    ///
    /// Combat commands act on the CombatController of the loaded CombatStage and go through its public
    /// verbs (Kill, Heal, DealDamage...), so every event and interceptor fires exactly as in a real fight.
    ///
    /// Selector syntax used by the combat.* commands:
    ///   all | allies | enemies | current | ally:left | enemy:mid | any part of a unit's name
    /// </summary>
    [Preserve]
    internal static class GameCommands
    {
        // ══ Combat: entering ═════════════════════════════════════════════════

        [ConsoleCommand("combat.start", "Starts a combat against the nearest active EnemyEntity. Optional: which team has the advantage.")]
        private static string StartCombat(UNIT_TEAM advantage = UNIT_TEAM.Ally)
        {
            if (UnityEngine.Object.FindFirstObjectByType<CombatController>() != null)
                return "A combat is already running.";
            if (CombatTransition.IsTransitionInProgress)
                return "A combat transition is already in progress.";

            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            if (player == null) return "No PlayerEntity in the loaded scenes (are you in the Overworld?).";

            EnemyEntity enemy = NearestEnemy(player.transform.position);
            if (enemy == null) return "No active EnemyEntity found. Use 'enemy.spawn' first.";

            // Hide first so the input map is restored before CombatTransition switches to Combat.
            DevConsole.Hide();
            CombatTransition.Begin(player.Party, enemy.EnemyParty, advantage, player.gameObject, enemy.gameObject);
            return $"Combat vs '{enemy.name}' (advantage: {advantage}).";
        }

        // ══ Combat: inside an encounter ══════════════════════════════════════

        [ConsoleCommand("combat.status", "Prints the roster, the acting unit and the upcoming turns.")]
        private static string CombatStatus()
        {
            if (!TryGetCombat(out CombatController combat, out string error)) return error;

            var sb = new StringBuilder("Roster:");
            foreach (Unit u in combat.Roster)
            {
                sb.Append($"\n  {u.Team,-5} {(u.Slot.HasValue ? u.Slot.Value.ToString() : "-"),-5} " +
                          $"{u.Name}  HP {u.HP}/{u.MaxHP}  SP {u.SP}/{u.MaxSP}{(u.IsAlive ? string.Empty : "  (dead)")}");
            }

            sb.Append("\nActing: ").Append(combat.CurrentActor != null ? combat.CurrentActor.Name : "-");
            sb.Append("\nUpcoming: ").Append(string.Join(" > ", combat.UpcomingTurns.Select(u => u.Name)));
            return sb.ToString();
        }

        [ConsoleCommand("combat.win", "Kills every living enemy (ends the combat as a Victory through the normal flow).")]
        private static string Win()
        {
            if (!TryGetCombat(out CombatController combat, out string error)) return error;

            foreach (Unit unit in combat.Roster.Where(u => u.Team == UNIT_TEAM.Enemy && u.IsAlive).ToList())
            {
                if (!combat.IsCombatActive) break;
                combat.Kill(unit);
            }

            return "Enemies defeated.";
        }

        [ConsoleCommand("combat.lose", "Kills every living ally (ends the combat as a Defeat).")]
        private static string Lose()
        {
            if (!TryGetCombat(out CombatController combat, out string error)) return error;

            foreach (Unit unit in combat.Roster.Where(u => u.Team == UNIT_TEAM.Ally && u.IsAlive).ToList())
            {
                if (!combat.IsCombatActive) break;
                combat.Kill(unit);
            }

            return "Allies defeated.";
        }

        [ConsoleCommand("combat.endturn", "Ends the current actor's turn without acting.")]
        private static string EndTurn()
        {
            if (!TryGetCombat(out CombatController combat, out string error)) return error;
            if (combat.CurrentActor == null) return "Nobody is acting right now.";

            string name = combat.CurrentActor.Name;
            combat.EndTurn();
            return $"Ended {name}'s turn.";
        }

        [ConsoleCommand("combat.kill", "Kills the selected living units.")]
        private static string Kill(string selector)
        {
            if (!TryGetUnits(selector, u => u.IsAlive, out CombatController combat, out List<Unit> units, out string error))
                return error;

            foreach (Unit unit in units)
            {
                if (!combat.IsCombatActive) break;
                combat.Kill(unit);
            }

            return $"Killed {units.Count} unit(s).";
        }

        [ConsoleCommand("combat.damage", "Deals damage to the selected units (goes through OnBeforeDamage like a real hit).")]
        private static string Damage(string selector, int amount, UNITY_TYPE type = UNITY_TYPE.Physical)
        {
            if (!TryGetUnits(selector, u => u.IsAlive, out CombatController combat, out List<Unit> units, out string error))
                return error;

            int total = 0;
            foreach (Unit unit in units) total += combat.DealDamage(null, unit, amount, type, "dev-console");
            return $"Dealt {total} damage across {units.Count} unit(s).";
        }

        [ConsoleCommand("combat.heal", "Heals the selected living units (default: to full).")]
        private static string Heal(string selector, int amount = 99999)
        {
            if (!TryGetUnits(selector, u => u.IsAlive, out CombatController combat, out List<Unit> units, out string error))
                return error;

            foreach (Unit unit in units) combat.Heal(null, unit, amount, "dev-console");
            return $"Healed {units.Count} unit(s).";
        }

        [ConsoleCommand("combat.sp", "Restores SP on the selected living units (default: to full).")]
        private static string RestoreSp(string selector, int amount = 99999)
        {
            if (!TryGetUnits(selector, u => u.IsAlive, out CombatController combat, out List<Unit> units, out string error))
                return error;

            foreach (Unit unit in units) combat.RestoreSP(unit, amount);
            return $"Restored SP on {units.Count} unit(s).";
        }

        [ConsoleCommand("combat.revive", "Revives the selected dead units with the given HP.")]
        private static string Revive(string selector, int hp = 1)
        {
            if (!TryGetUnits(selector, u => !u.IsAlive, out CombatController combat, out List<Unit> units, out string error))
                return error;

            foreach (Unit unit in units) combat.Revive(unit, hp);
            return $"Revived {units.Count} unit(s).";
        }

        // ══ Enemies (Overworld) ══════════════════════════════════════════════

        [ConsoleCommand("enemy.list", "Lists the active EnemyEntity objects and their distance to the player.")]
        private static string ListEnemies()
        {
            EnemyEntity[] enemies = UnityEngine.Object.FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            if (enemies.Length == 0) return "No active EnemyEntity.";

            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            var sb = new StringBuilder($"{enemies.Length} active enemy entity(ies):");
            foreach (EnemyEntity e in enemies.OrderBy(e => player != null
                         ? Vector3.Distance(player.transform.position, e.transform.position)
                         : 0f))
            {
                string distance = player != null
                    ? $"{Vector3.Distance(player.transform.position, e.transform.position):0.0}m"
                    : "?";
                sb.Append($"\n  {e.name}  ({distance}, {e.EnemyParty.Slots.Count} slot(s))");
            }

            return sb.ToString();
        }

        [ConsoleCommand("enemy.spawn", "Clones the nearest active EnemyEntity in front of the player.")]
        private static string SpawnEnemy(int count = 1, float distance = 5f)
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            if (player == null) return "No PlayerEntity in the loaded scenes.";

            EnemyEntity template = NearestEnemy(player.transform.position);
            if (template == null)
                return "No active EnemyEntity to clone. Place one in the scene (or add a prefab registry).";

            count = Mathf.Clamp(count, 1, 10);
            int spawned = 0;

            for (int i = 0; i < count; i++)
            {
                // Fan the clones out sideways so they do not overlap.
                float sideways = (i - (count - 1) * 0.5f) * 2f;
                Vector3 wanted = player.transform.position
                                 + player.transform.forward * distance
                                 + player.transform.right * sideways;

                if (!NavMesh.SamplePosition(wanted, out NavMeshHit hit, 4f, NavMesh.AllAreas)) continue;

                Quaternion facing = Quaternion.LookRotation(
                    Vector3.ProjectOnPlane(player.transform.position - hit.position, Vector3.up));

                // Same parent as the template so room activation (DungeonRoom) keeps managing the clone.
                GameObject clone = UnityEngine.Object.Instantiate(
                    template.gameObject, hit.position, facing, template.transform.parent);
                clone.name = template.name + " (dev)";
                spawned++;
            }

            return spawned == count
                ? $"Spawned {spawned} x '{template.name}'."
                : $"Spawned {spawned}/{count} (the rest had no NavMesh near the target point).";
        }

        [ConsoleCommand("enemy.clear", "Destroys every active EnemyEntity in the Overworld.")]
        private static string ClearEnemies()
        {
            EnemyEntity[] enemies = UnityEngine.Object.FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            foreach (EnemyEntity e in enemies) UnityEngine.Object.Destroy(e.gameObject);
            return $"Destroyed {enemies.Length} enemy entity(ies).";
        }

        // ══ Party ════════════════════════════════════════════════════════════

        [ConsoleCommand("party.list", "Lists the party members in order (only the first 3 fight).")]
        private static string PartyList()
        {
            if (!TryGetParty(out AllyParty party, out string error)) return error;
            if (party.Members.Count == 0) return "The party is empty.";

            var sb = new StringBuilder("Party:");
            for (int i = 0; i < party.Members.Count; i++)
            {
                PartyMemberData m = party.Members[i];
                if (m == null || m.definition == null)
                {
                    sb.Append($"\n  [{i}] (empty)");
                    continue;
                }

                string hp = m.currentHP < 0 ? "full" : m.currentHP.ToString();
                string sp = m.currentSP < 0 ? "full" : m.currentSP.ToString();
                sb.Append($"\n  [{i}] {m.definition.unitID} ({m.definition.name})  HP {hp}  SP {sp}" +
                          (i >= CombatSlots.MaxPerTeam ? "  [bench]" : string.Empty));
            }

            return sb.ToString();
        }

        [ConsoleCommand("party.add", "Adds a character to the party by unitID or asset name (must be a loaded definition).")]
        private static string PartyAdd(string unit)
        {
            if (!TryGetParty(out AllyParty party, out string error)) return error;
            if (!TryFindDefinition(unit, out UnitDefinitionSO definition, out error)) return error;

            if (party.Contains(definition)) return $"'{definition.unitID}' is already in the party.";

            party.TryAdd(new PartyMemberData { definition = definition });
            return party.Contains(definition)
                ? $"Added '{definition.unitID}'."
                : $"Could not add '{definition.unitID}' (party full?).";
        }

        [ConsoleCommand("party.heal", "Resets HP and SP of a member (index) or of everyone (all) to full for the next combat.")]
        private static string PartyHeal(string target = "all")
        {
            if (!TryGetParty(out AllyParty party, out string error)) return error;

            int healed = 0;
            for (int i = 0; i < party.Members.Count; i++)
            {
                if (target != "all" && !(int.TryParse(target, out int index) && index == i)) continue;

                PartyMemberData m = party.Members[i];
                if (m == null) continue;

                m.currentHP = -1; // -1 = start the next combat at max
                m.currentSP = -1;
                healed++;
            }

            return $"Reset HP/SP of {healed} member(s).";
        }

        [ConsoleCommand("party.hp", "Sets the persistent HP of a party member by index (0 = starts the next combat dead).")]
        private static string PartySetHp(int index, int hp)
        {
            if (!TryGetParty(out AllyParty party, out string error)) return error;
            if (index < 0 || index >= party.Members.Count || party.Members[index] == null)
                return $"No party member at index {index}.";

            party.Members[index].currentHP = Mathf.Max(0, hp);
            return $"Member [{index}] HP = {party.Members[index].currentHP}.";
        }

        // ══ Challenges ═══════════════════════════════════════════════════════

        [ConsoleCommand("challenge.list", "Lists every challenge with its progress.")]
        private static string ChallengeList()
        {
            if (!TryGetChallenges(out ChallengeManager manager, out string error)) return error;

            var sb = new StringBuilder("Challenges:");
            foreach (ChallengeSO c in manager.Catalog.Challenges)
            {
                if (c == null) continue;

                (int current, int target) = manager.GetProgress(c);
                sb.Append($"\n  [{(manager.IsCompleted(c.challengeID) ? "x" : " ")}] {c.challengeID}  {current}/{target}");
            }

            return sb.ToString();
        }

        [ConsoleCommand("challenge.unlock", "Completes a challenge by ID (or 'all'), granting its rewards.")]
        private static string ChallengeUnlock(string id)
        {
            if (!TryGetChallenges(out ChallengeManager manager, out string error)) return error;

            if (string.Equals(id, "all", StringComparison.OrdinalIgnoreCase))
            {
                int completed = 0;
                foreach (ChallengeSO c in manager.Catalog.Challenges.ToList())
                    if (c != null && manager.ForceComplete(c.challengeID)) completed++;
                return $"Completed {completed} challenge(s).";
            }

            return manager.ForceComplete(id)
                ? $"Completed '{id}'."
                : $"'{id}' does not exist or is already completed.";
        }

        [ConsoleCommand("challenge.stat", "Reports progress for a challenge stat (as gameplay would).")]
        private static string ReportStat(CHALLENGE_STAT stat, int amount = 1)
        {
            if (amount <= 0) return "Amount must be positive.";

            ChallengeEvents.ReportStat(stat, amount);
            return $"Reported {stat} +{amount}.";
        }

        [ConsoleCommand("challenge.milestone", "Reports a challenge milestone by ID.")]
        private static string ReportMilestone(string milestoneId)
        {
            ChallengeEvents.ReportMilestone(milestoneId);
            return $"Reported milestone '{milestoneId}'.";
        }

        // ══ Helpers ══════════════════════════════════════════════════════════

        private static EnemyEntity NearestEnemy(Vector3 from)
        {
            EnemyEntity[] enemies = UnityEngine.Object.FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            return enemies.Length == 0
                ? null
                : enemies.OrderBy(e => (e.transform.position - from).sqrMagnitude).First();
        }

        private static bool TryGetCombat(out CombatController combat, out string error)
        {
            combat = UnityEngine.Object.FindFirstObjectByType<CombatController>();
            error = combat != null && combat.IsCombatActive ? null : "There is no active combat.";
            return error == null;
        }

        private static bool TryGetParty(out AllyParty party, out string error)
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEntity>();
            party = player != null ? player.Party : null;
            error = party != null ? null : "No PlayerEntity in the loaded scenes (are you in the Overworld?).";
            return error == null;
        }

        private static bool TryGetChallenges(out ChallengeManager manager, out string error)
        {
            manager = ChallengeManager.Instance;
            error = manager != null && manager.Catalog != null
                ? null
                : "ChallengeManager is not available (boot scene not loaded?).";
            return error == null;
        }

        private static bool TryFindDefinition(string query, out UnitDefinitionSO definition, out string error)
        {
            definition = null;
            error = null;

            // Only definitions already in memory are found: the recruiter list, the party and the enemies
            // of the open scenes. A UnitCatalog asset would make this complete in builds.
            UnitDefinitionSO[] all = Resources.FindObjectsOfTypeAll<UnitDefinitionSO>();

            UnitDefinitionSO exact = all.FirstOrDefault(d =>
                string.Equals(d.unitID, query, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.name, query, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                definition = exact;
                return true;
            }

            List<UnitDefinitionSO> partial = all
                .Where(d => d.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (d.unitID != null && d.unitID.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            if (partial.Count == 1)
            {
                definition = partial[0];
                return true;
            }

            error = partial.Count == 0
                ? $"No loaded unit definition matches '{query}'."
                : "Ambiguous, matches: " + string.Join(", ", partial.Select(d => d.unitID ?? d.name));
            return false;
        }

        /// <summary>Resolves a selector to units of the running combat and filters them with the predicate.</summary>
        private static bool TryGetUnits(string selector, Func<Unit, bool> filter,
            out CombatController combat, out List<Unit> units, out string error)
        {
            units = null;
            if (!TryGetCombat(out combat, out error)) return false;

            IEnumerable<Unit> selected = SelectUnits(combat, selector);
            units = selected.Where(filter).ToList();

            if (units.Count == 0)
            {
                error = $"Selector '{selector}' matched no suitable unit. " +
                        "Use: all | allies | enemies | current | ally:left | enemy:mid | part of a name.";
                return false;
            }

            return true;
        }

        private static IEnumerable<Unit> SelectUnits(CombatController combat, string selector)
        {
            string s = selector.Trim().ToLowerInvariant();
            IReadOnlyList<Unit> roster = combat.Roster;

            switch (s)
            {
                case "all": return roster;
                case "ally": case "allies": return roster.Where(u => u.Team == UNIT_TEAM.Ally);
                case "enemy": case "enemies": return roster.Where(u => u.Team == UNIT_TEAM.Enemy);
                case "current": return combat.CurrentActor != null ? new[] { combat.CurrentActor } : Array.Empty<Unit>();
            }

            // team:slot, e.g. enemy:left
            string[] parts = s.Split(':');
            if (parts.Length == 2 &&
                (parts[0] == "ally" || parts[0] == "enemy") &&
                Enum.TryParse(parts[1], true, out COMBAT_SLOT slot))
            {
                UNIT_TEAM team = parts[0] == "ally" ? UNIT_TEAM.Ally : UNIT_TEAM.Enemy;
                return roster.Where(u => u.Team == team && u.Slot == slot);
            }

            return roster.Where(u => u.Name != null && u.Name.ToLowerInvariant().Contains(s));
        }
    }
}
#endif