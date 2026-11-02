document.querySelectorAll('time[data-local]').forEach(t => {
    t.textContent = new Date(t.dateTime).toLocaleString([], { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
});
