// Front Theme Toggle - shared for all front pages (Landing, Login, Register)
// Uses the same storage key pattern as Helpers/getStoredTheme so the theme
// persists entre páginas.

document.addEventListener('DOMContentLoaded', function () {
  const themeButtons = document.querySelectorAll('[data-bs-theme-value]');
  const activeThemeIcon = document.querySelector('.theme-icon-active');
  if (!themeButtons.length) return;

  const templateName = window.templateName || 'front-page';
  const storageKey = `templateCustomizer-${templateName}--Theme`;

  function applyTheme(theme, updateStorage = true) {
    let appliedTheme = theme;
    if (theme === 'system') {
      appliedTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    document.documentElement.setAttribute('data-bs-theme', appliedTheme);

    if (updateStorage) {
      try {
        localStorage.setItem(storageKey, theme);
      } catch (e) {}
    }

    themeButtons.forEach(btn => btn.classList.remove('active'));
    const currentBtn = document.querySelector(`[data-bs-theme-value="${theme}"]`);
    if (currentBtn) {
      currentBtn.classList.add('active');
      const iconEl = currentBtn.querySelector('i[data-icon]');
      if (activeThemeIcon && iconEl) {
        const iconName = iconEl.getAttribute('data-icon');
        activeThemeIcon.className = `icon-base ri ri-${iconName} icon-md theme-icon-active`;
      }
    }
  }

  // aplicar tema salvo ou padrão logo no load
  let storedTheme = null;
  try {
    storedTheme = localStorage.getItem(storageKey);
  } catch (e) {}
  if (!storedTheme) storedTheme = 'light';
  applyTheme(storedTheme, false);

  themeButtons.forEach(btn => {
    btn.addEventListener('click', function () {
      const theme = this.getAttribute('data-bs-theme-value') || 'light';
      applyTheme(theme, true);
    });
  });
});

