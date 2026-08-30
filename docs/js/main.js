// Hunters: Fracture in Time — docs
// Sitio multipágina estático: el único JS necesario es el toggle del sidebar en móvil.
// El estado "activo" del nav se resuelve por página con aria-current="page" en el HTML.

(function () {
  const sidebar = document.getElementById('sidebar');
  const toggle = document.getElementById('navToggle');

  if (!toggle || !sidebar) return;

  toggle.addEventListener('click', () => {
    const isOpen = sidebar.classList.toggle('open');
    toggle.setAttribute('aria-expanded', String(isOpen));
  });

  sidebar.querySelectorAll('a').forEach((link) => {
    link.addEventListener('click', () => {
      sidebar.classList.remove('open');
      toggle.setAttribute('aria-expanded', 'false');
    });
  });
})();
