#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using Core;
using Core.Shop;

namespace DevTools
{
    /// <summary>Dev console commands to test the economy and the shop.</summary>
    [Preserve]
    internal static class ShopCommands
    {
        private const string NoGameManager = "GameManager is not available yet.";
        private static readonly CultureInfo Clp = new("es-CL");

        private static string Fmt(float amount) => "$ " + amount.ToString("N0", Clp);

        // ── Pesos ─────────────────────────────────────────────────────────────

        [ConsoleCommand("pesos", "Shows the current pesos balance.")]
        private static string Pesos() =>
            GameManager.Instance == null ? NoGameManager : $"Pesos: {Fmt(GameManager.Instance.Pesos)}";

        [ConsoleCommand("pesos.add", "Adds pesos to the balance.")]
        private static string AddPesos(float amount)
        {
            if (GameManager.Instance == null) return NoGameManager;
            if (amount <= 0f) return "Amount must be greater than 0. Use 'pesos.remove' to subtract.";

            GameManager.Instance.AddBalance(amount);
            return $"+{Fmt(amount)} -> {Fmt(GameManager.Instance.Pesos)}";
        }

        [ConsoleCommand("pesos.remove", "Removes pesos from the balance (fails if there are not enough).")]
        private static string RemovePesos(float amount)
        {
            if (GameManager.Instance == null) return NoGameManager;
            if (amount <= 0f) return "Amount must be greater than 0. Use 'pesos.add' to add.";

            if (!GameManager.Instance.TrySpendPesos(amount))
                return $"Not enough pesos (have {Fmt(GameManager.Instance.Pesos)}). Use 'pesos.set 0' to empty it.";

            return $"-{Fmt(amount)} -> {Fmt(GameManager.Instance.Pesos)}";
        }

        [ConsoleCommand("pesos.set", "Sets the balance to an exact amount.")]
        private static string SetPesos(float amount)
        {
            if (GameManager.Instance == null) return NoGameManager;
            if (amount < 0f) return "Amount cannot be negative.";

            GameManager gm = GameManager.Instance;
            float current = gm.Pesos;

            if (amount >= current) gm.AddBalance(amount - current);
            else gm.TrySpendPesos(current - amount);

            return $"Pesos = {Fmt(gm.Pesos)}";
        }

        // ── Shop ──────────────────────────────────────────────────────────────

        [ConsoleCommand("shop.catalogs", "Lists the shop catalogs currently loaded in memory.")]
        private static string ListCatalogs()
        {
            ShopCatalogSO[] catalogs = LoadedCatalogs();
            if (catalogs.Length == 0) return "No ShopCatalogSO is loaded (assign one to an NPC or the ShopController).";

            return "Catalogs:\n  " + string.Join("\n  ", catalogs.Select(c => $"{c.name} ({c.Entries.Count} entries)"));
        }

        [ConsoleCommand("shop.open", "Opens a shop catalog by (partial) name. Close the console (F4) to use it.")]
        private static string OpenShop(string catalog = null)
        {
            ShopController controller = UnityEngine.Object.FindFirstObjectByType<ShopController>();
            if (controller == null) return "No active ShopController in the scene.";

            ShopCatalogSO[] catalogs = LoadedCatalogs();
            if (catalogs.Length == 0) return "No ShopCatalogSO is loaded.";

            ShopCatalogSO target;

            if (string.IsNullOrWhiteSpace(catalog))
            {
                if (catalogs.Length > 1)
                    return "Several catalogs are loaded, pick one:\n  " + string.Join("\n  ", catalogs.Select(c => c.name));

                target = catalogs[0];
            }
            else
            {
                target = catalogs.FirstOrDefault(c => string.Equals(c.name, catalog, StringComparison.OrdinalIgnoreCase));

                if (target == null)
                {
                    ShopCatalogSO[] partial = catalogs
                        .Where(c => c.name.IndexOf(catalog, StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToArray();

                    if (partial.Length == 0) return $"No catalog matches '{catalog}'. Use 'shop.catalogs'.";
                    if (partial.Length > 1) return "Ambiguous, matches: " + string.Join(", ", partial.Select(c => c.name));

                    target = partial[0];
                }
            }

            controller.Open(target);
            return $"Opened '{target.name}'.";
        }

        [ConsoleCommand("shop.close", "Closes the shop if it is open.")]
        private static string CloseShop()
        {
            ShopController controller = UnityEngine.Object.FindFirstObjectByType<ShopController>();
            if (controller == null) return "No active ShopController in the scene.";
            if (!controller.IsOpen) return "The shop is already closed.";

            controller.Close();
            return "Shop closed.";
        }

        [ConsoleCommand("shop.restock", "Resets the stock of every shop to its catalog values.")]
        private static string Restock()
        {
            ShopController controller = UnityEngine.Object.FindFirstObjectByType<ShopController>();
            if (controller == null) return "No active ShopController in the scene.";

            controller.ResetStock();
            return "Shop stock reset.";
        }

        private static ShopCatalogSO[] LoadedCatalogs() =>
            Resources.FindObjectsOfTypeAll<ShopCatalogSO>().OrderBy(c => c.name).ToArray();
    }
}
#endif