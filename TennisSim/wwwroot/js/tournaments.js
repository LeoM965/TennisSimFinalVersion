function applyFilters() {
    const filters = getFilterValues();
    const cards = document.querySelectorAll('.tournament-card');
    const monthHeaders = document.querySelectorAll('.month-header');
    const grids = document.querySelectorAll('.tournament-grid');

    filterCards(cards, filters);
    updateMonthVisibility(grids, monthHeaders);
}

function resetFilters() {
    clearFilterInputs();
    showAllElements();
}

function getFilterValues() {
    return {
        category: document.getElementById('category').value,
        surface: document.getElementById('surface').value,
        country: document.getElementById('country').value,
        dateFrom: document.getElementById('date-from').value,
        dateTo: document.getElementById('date-to').value
    };
}

function filterCards(cards, filters) {
    cards.forEach(card => {
        const shouldShow = cardMatchesFilters(card, filters);
        card.style.display = shouldShow ? 'block' : 'none';
    });
}

function cardMatchesFilters(card, filters) {
    const cardData = {
        category: card.dataset.category,
        surface: card.dataset.surface,
        country: card.dataset.country,
        startDate: card.dataset.startDate
    };

    return matchesCategory(cardData.category, filters.category) &&
        matchesSurface(cardData.surface, filters.surface) &&
        matchesCountry(cardData.country, filters.country) &&
        matchesDateRange(cardData.startDate, filters.dateFrom, filters.dateTo);
}

function matchesCategory(cardCategory, filterCategory) {
    return !filterCategory || cardCategory === filterCategory;
}

function matchesSurface(cardSurface, filterSurface) {
    return !filterSurface || cardSurface === filterSurface;
}

function matchesCountry(cardCountry, filterCountry) {
    return !filterCountry || cardCountry === filterCountry;
}

function matchesDateRange(cardDate, dateFrom, dateTo) {
    const matchesFrom = !dateFrom || cardDate >= dateFrom;
    const matchesTo = !dateTo || cardDate <= dateTo;
    return matchesFrom && matchesTo;
}

function updateMonthVisibility(grids, monthHeaders) {
    grids.forEach((grid, index) => {
        const visibleCards = getVisibleCards(grid);
        const header = monthHeaders[index];

        if (visibleCards.length === 0) {
            hideMonthSection(header, grid);
        } else {
            showMonthSection(header, grid);
        }
    });
}

function getVisibleCards(grid) {
    return grid.querySelectorAll('.tournament-card[style*="block"], .tournament-card:not([style*="none"])');
}

function hideMonthSection(header, grid) {
    header.style.display = 'none';
    grid.style.display = 'none';
}

function showMonthSection(header, grid) {
    header.style.display = 'flex';
    grid.style.display = 'grid';
}

function clearFilterInputs() {
    const filterIds = ['category', 'surface', 'country', 'date-from', 'date-to'];
    filterIds.forEach(id => {
        document.getElementById(id).value = '';
    });
}

function showAllElements() {
    showAllCards();
    showAllMonthHeaders();
    showAllGrids();
}

function showAllCards() {
    document.querySelectorAll('.tournament-card').forEach(card => {
        card.style.display = 'block';
    });
}

function showAllMonthHeaders() {
    document.querySelectorAll('.month-header').forEach(header => {
        header.style.display = 'flex';
    });
}

function showAllGrids() {
    document.querySelectorAll('.tournament-grid').forEach(grid => {
        grid.style.display = 'grid';
    });
}