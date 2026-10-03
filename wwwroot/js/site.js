document.addEventListener('DOMContentLoaded', () => {
  const sidebar = document.getElementById('sidebar');
  const scrim   = document.getElementById('scrim');
  document.getElementById('menuButton')?.addEventListener('click', () => { sidebar?.classList.add('open');  if (scrim) scrim.style.display = 'block'; });
  document.getElementById('mobileClose')?.addEventListener('click', () => { sidebar?.classList.remove('open'); if (scrim) scrim.style.display = 'none'; });
  scrim?.addEventListener('click', () => { sidebar?.classList.remove('open'); if (scrim) scrim.style.display = 'none'; });

  document.querySelector('[data-open-patient]')?.addEventListener('click', () => {
    document.getElementById('patientDialog')?.showModal();
  });
});