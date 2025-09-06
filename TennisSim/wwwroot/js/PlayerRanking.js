document.addEventListener('DOMContentLoaded', function () {
    const viewButtons = document.querySelectorAll('.btn-view');
    viewButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            this.innerHTML = 'Loading...';
            this.style.opacity = '0.7';
        });
    });

    const searchInput = document.createElement('input');
    searchInput.type = 'text';
    searchInput.placeholder = 'Search players...';
    searchInput.style.cssText = 'width:300px;padding:10px;margin:20px auto;display:block;border:2px solid #ddd;border-radius:8px;font-size:16px';

    const h2 = document.querySelector('h2');
    const table = document.querySelector('.rankings-table');
    if (h2 && table) h2.parentNode.insertBefore(searchInput, table);

    searchInput.addEventListener('input', function () {
        const searchTerm = this.value.toLowerCase();
        const rows = document.querySelectorAll('tbody tr');
        rows.forEach(row => {
            const playerName = row.querySelector('.player-name').textContent.toLowerCase();
            const countrySpan = row.querySelector('.country span');
            const country = countrySpan ? countrySpan.textContent.toLowerCase() : '';
            row.style.display = (playerName.includes(searchTerm) || country.includes(searchTerm)) ? 'table-row' : 'none';
        });
    });

    const playerNames = document.querySelectorAll('.player-name');
    playerNames.forEach(name => {
        name.style.cssText = 'cursor:pointer';
        name.title = 'Click to copy';
        name.addEventListener('click', function () {
            const text = this.textContent.trim();
            const copyText = () => {
                const original = this.textContent;
                this.textContent = 'Copied!';
                this.style.color = '#059669';
                setTimeout(() => {
                    this.textContent = original;
                    this.style.color = '#2d3748';
                }, 1500);
            };

            if (navigator.clipboard) {
                navigator.clipboard.writeText(text).then(copyText);
            } else {
                const textarea = document.createElement('textarea');
                textarea.value = text;
                document.body.appendChild(textarea);
                textarea.select();
                document.execCommand('copy');
                document.body.removeChild(textarea);
                copyText();
            }
        });
    });
});