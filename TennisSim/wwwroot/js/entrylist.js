document.addEventListener('DOMContentLoaded', function () {
    const playerSearch = document.getElementById('playerSearch');
    const sortSelect = document.getElementById('sortSelect');
    const tableBody = document.getElementById('entryListBody');
    const originalRows = Array.from(tableBody.getElementsByTagName('tr'));

    function filterAndSortRows() {
        let rows = [...originalRows];

        if (playerSearch.value) {
            const searchTerm = playerSearch.value.toLowerCase();
            rows = rows.filter(row => {
                const playerName = row.getElementsByTagName('td')[1].textContent.toLowerCase();
                return playerName.includes(searchTerm);
            });
        }

        rows.sort((a, b) => {
            const aValue = getSortValue(a);
            const bValue = getSortValue(b);
            if (sortSelect.value === 'name') {
                return aValue.localeCompare(bValue);
            }
            return parseFloat(aValue) - parseFloat(bValue);
        });

        tableBody.innerHTML = '';
        rows.forEach(row => tableBody.appendChild(row));
        animateRows();
    }

    function getSortValue(row) {
        const cells = row.getElementsByTagName('td');
        switch (sortSelect.value) {
            case 'rank':
                return cells[0].textContent.trim();
            case 'name':
                return cells[1].textContent.trim();
            case 'points':
                return cells[2].textContent.trim().replace(/,/g, '');
            default:
                return '';
        }
    }

    function animateRows() {
        const rows = tableBody.querySelectorAll('tr');
        rows.forEach((row, index) => {
            row.style.animationDelay = (index * 0.1) + 's';
            row.style.animationPlayState = 'running';
        });
    }

    playerSearch.addEventListener('input', filterAndSortRows);
    sortSelect.addEventListener('change', filterAndSortRows);

    animateRows();
});