/**
 * Main - Front Pages
 */
'use strict';

window.isRtl = window.Helpers.isRtl();
window.isDarkStyle = window.Helpers.isDarkStyle();

(function () {
  // Button & Pagination Waves effect
  if (typeof Waves !== 'undefined') {
    Waves.init();
    Waves.attach(".btn[class*='btn-']:not([class*='btn-outline-']):not([class*='btn-label-'])", ['waves-light']);
    Waves.attach("[class*='btn-outline-']");
    Waves.attach("[class*='btn-label-']");
    Waves.attach('.pagination .page-item .page-link');
  }

  const menu = document.getElementById('navbarSupportedContent'),
    nav = document.querySelector('.layout-navbar'),
    navItemLink = document.querySelectorAll('.navbar-nav .nav-link');

  // Initialised custom options if checked
  setTimeout(function () {
    window.Helpers.initCustomOptionCheck();
  }, 1000);

  // Init BS Tooltip
  const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
  tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
  });

  if (isRtl) {
    // If layout is RTL add .dropdown-menu-end class to .dropdown-menu
    Helpers._addClass('dropdown-menu-end', document.querySelectorAll('#layout-navbar .dropdown-menu'));
    // If layout is RTL add .dropdown-menu-end class to .dropdown-menu
    Helpers._addClass('dropdown-menu-end', document.querySelectorAll('.dropdown-menu'));
  }

  // Navbar
  window.addEventListener('scroll', e => {
    if (window.scrollY > 10) {
      nav.classList.add('navbar-active');
    } else {
      nav.classList.remove('navbar-active');
    }
  });
  window.addEventListener('load', e => {
    if (window.scrollY > 10) {
      nav.classList.add('navbar-active');
    } else {
      nav.classList.remove('navbar-active');
    }
  });

  // Function to close the mobile menu
  function closeMenu() {
    if (menu) {
      menu.classList.remove('show');
    }
  }

  document.addEventListener('click', function (event) {
    // Check if the clicked element is inside mobile menu
    if (menu && !menu.contains(event.target)) {
      closeMenu();
    }
  });
  navItemLink.forEach(link => {
    link.addEventListener('click', event => {
      if (!link.classList.contains('dropdown-toggle')) {
        closeMenu();
      } else {
        event.preventDefault();
      }
    });
  });

  // Mega dropdown
  const megaDropdown = document.querySelectorAll('.nav-link.mega-dropdown');
  if (megaDropdown) {
    megaDropdown.forEach(e => {
      new MegaDropdown(e);
    });
  }

  // Get style from local storage or use 'light' as default
  let storedStyle =
    localStorage.getItem('templateCustomizer-' + (window.templateName || 'front-page') + '--Theme') || //if no template style then use Customizer style
    (window.templateCustomizer?.settings?.defaultTheme ?? document.documentElement.getAttribute('data-bs-theme')) || //!if there is no Customizer then use default style
    'light'; // final fallback
  console.log('Front-Main: storedStyle identified as:', storedStyle);

  let styleSwitcher = document.querySelector('.dropdown-style-switcher');
  let styleSwitcherIcon = styleSwitcher ? styleSwitcher.querySelector('i') : null;

  if (styleSwitcherIcon && storedStyle) {
    new bootstrap.Tooltip(styleSwitcherIcon, {
      title: storedStyle.charAt(0).toUpperCase() + storedStyle.slice(1) + ' Mode',
      fallbackPlacements: ['bottom']
    });
  }

  // Run switchImage function based on the stored style
  window.Helpers.switchImage(storedStyle);

  // Update light/dark image based on current style
  window.Helpers.setTheme(window.Helpers.getPreferredTheme());

  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    const storedTheme = window.Helpers.getStoredTheme();
    if (storedTheme !== 'light' && storedTheme !== 'dark') {
      window.Helpers.setTheme(window.Helpers.getPreferredTheme());
    }
  });

  function getScrollbarWidth() {
    const scrollbarWidth = window.innerWidth - document.documentElement.clientWidth;
    document.body.style.setProperty('--bs-scrollbar-width', `${scrollbarWidth}px`);
  }
  getScrollbarWidth();

  //Style Switcher (Light/Dark/System Mode)
  let initThemeSwitcher = () => {
    const theme = window.Helpers.getStoredTheme(window.templateName) || window.Helpers.getPreferredTheme();
    window.Helpers.setTheme(theme);
    window.Helpers.showActiveTheme(theme);
    getScrollbarWidth();
    const toggles = document.querySelectorAll('[data-bs-theme-value]');
    toggles.forEach(toggle => {
      toggle.addEventListener('click', () => {
        const theme = toggle.getAttribute('data-bs-theme-value');
        window.Helpers.setStoredTheme(window.templateName, theme);
        window.Helpers.setTheme(theme);
        window.Helpers.showActiveTheme(theme, true);
        window.Helpers.syncCustomOptions(theme);
        let currTheme = theme;
        if (theme === 'system') {
          currTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        }
        const styleSwitcher = document.querySelector('.dropdown-style-switcher');
        const icon = styleSwitcher ? styleSwitcher.querySelector('i') : null;
        if (icon) {
          if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
            try {
              const tooltip = bootstrap.Tooltip.getInstance(icon);
              if (tooltip) {
                tooltip.dispose();
              }
              new bootstrap.Tooltip(icon, {
                title: theme.charAt(0).toUpperCase() + theme.slice(1) + ' Mode',
                fallbackPlacements: ['bottom']
              });
            } catch (e) { }
          }
        }
        window.Helpers.switchImage(currTheme);
      });
    });
  };

  if (document.readyState === 'loading') {
    window.addEventListener('DOMContentLoaded', initThemeSwitcher);
  } else {
    initThemeSwitcher();
  }

  // Accordion active class and previous-active class
  const accordionActiveFunction = function (e) {
    if (e.type == 'show.bs.collapse' || e.type == 'show.bs.collapse') {
      e.target.closest('.accordion-item').classList.add('active');
      e.target.closest('.accordion-item').previousElementSibling?.classList.add('previous-active');
    } else {
      e.target.closest('.accordion-item').classList.remove('active');
      e.target.closest('.accordion-item').previousElementSibling?.classList.remove('previous-active');
    }
  };

  const accordionTriggerList = [].slice.call(document.querySelectorAll('.accordion'));
  const accordionList = accordionTriggerList.map(function (accordionTriggerEl) {
    accordionTriggerEl.addEventListener('show.bs.collapse', accordionActiveFunction);
    accordionTriggerEl.addEventListener('hide.bs.collapse', accordionActiveFunction);
  });
  // Initialize Password Toggle
  window.Helpers.initPasswordToggle();
})();
