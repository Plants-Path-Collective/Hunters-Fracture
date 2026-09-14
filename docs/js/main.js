/* ============================================================
   Hunters: Fracture of Time — Docs
   JS compartido para el hub y todas las páginas de módulo.
   Cada bloque se auto-guarda: chequea si sus elementos existen
   antes de correr, así el mismo archivo sirve para cualquier página.
   ============================================================ */
(function () {
  "use strict";

  /* ------------------------------------------------------------
     1. THEME TOGGLE (todas las páginas)
     ------------------------------------------------------------ */
  const root = document.documentElement;
  const themeBtn = document.getElementById("themeToggle");
  const STORAGE_KEY = "hunters-docs-theme";

  function applyTheme(theme) {
    root.dataset.theme = theme;
    if (themeBtn) {
      themeBtn.textContent = theme === "dark" ? "☀ Claro" : "☾ Oscuro";
    }
    try {
      localStorage.setItem(STORAGE_KEY, theme);
    } catch (e) { /* storage bloqueado, ignorar */ }
  }

  function readStoredTheme() {
    try {
      return localStorage.getItem(STORAGE_KEY);
    } catch (e) {
      return null;
    }
  }

  applyTheme(readStoredTheme() || "dark");

  if (themeBtn) {
    themeBtn.addEventListener("click", function () {
      applyTheme(root.dataset.theme === "dark" ? "light" : "dark");
    });
  }

  /* ------------------------------------------------------------
     2. SIDEBAR MÓVIL
     ------------------------------------------------------------ */
  const sidebar = document.getElementById("sidebar");
  const navToggle = document.getElementById("navToggle");

  if (navToggle && sidebar) {
    navToggle.addEventListener("click", function () {
      const isOpen = sidebar.classList.toggle("open");
      navToggle.setAttribute("aria-expanded", String(isOpen));
    });

    sidebar.querySelectorAll("a").forEach(function (link) {
      link.addEventListener("click", function () {
        sidebar.classList.remove("open");
        navToggle.setAttribute("aria-expanded", "false");
      });
    });
  }

  /* ------------------------------------------------------------
     3. TOC SCROLL-SPY (solo en páginas con tabla de contenidos)
     ------------------------------------------------------------ */
  const tocLinks = Array.from(document.querySelectorAll(".toc a"));
  const tocSections = tocLinks
    .map(function (a) {
      const id = (a.getAttribute("href") || "").replace("#", "");
      return document.getElementById(id);
    })
    .filter(Boolean);

  function setActiveToc(id) {
    tocLinks.forEach(function (a) {
      a.classList.toggle("active", a.getAttribute("href") === "#" + id);
    });
  }

  function spy() {
    if (!tocSections.length) return;
    let current = tocSections[0].id;
    for (const s of tocSections) {
      if (s.getBoundingClientRect().top <= 120) current = s.id;
    }
    setActiveToc(current);
  }

  if (tocSections.length) {
    document.addEventListener("scroll", spy, { passive: true });
    window.addEventListener("load", spy);
    spy();
  }

  /* ------------------------------------------------------------
     4. FILTRO DEL HUB (solo en index.html)
     ------------------------------------------------------------ */
  const filterInput = document.getElementById("filterInput");
  const headerSearch = document.getElementById("headerSearch");
  const cards = Array.from(document.querySelectorAll(".index-card"));
  const emptyState = document.getElementById("emptyState");

  if (cards.length && filterInput && emptyState) {
    function applyFilter(raw) {
      const q = (raw || "").trim().toLowerCase();
      let shown = 0;

      cards.forEach(function (card) {
        const haystack = (
          (card.dataset.keywords || "") + " " + card.textContent
        ).toLowerCase();
        const match = !q || haystack.includes(q);
        card.classList.toggle("hidden", !match);
        if (match) shown++;
      });

      emptyState.classList.toggle("hidden", shown > 0);
    }

    function syncFilter(value, source) {
      if (source !== filterInput) filterInput.value = value;
      if (source !== headerSearch && headerSearch) headerSearch.value = value;
      applyFilter(value);
    }

    filterInput.addEventListener("input", function (e) {
      syncFilter(e.target.value, filterInput);
    });

    if (headerSearch) {
      headerSearch.addEventListener("input", function (e) {
        syncFilter(e.target.value, headerSearch);
      });
    }

    document.addEventListener("keydown", function (e) {
      if (e.key !== "/") return;
      const active = document.activeElement;
      if (active === filterInput || active === headerSearch) return;
      if (active && /^(INPUT|TEXTAREA|SELECT)$/.test(active.tagName)) return;
      e.preventDefault();
      filterInput.focus();
    });
  }
})();
