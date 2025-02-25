document.addEventListener('DOMContentLoaded', async function () {
    const repoUrl = 'https://api.github.com/repos/Nonsultant/specgurka';
    const latestReleaseUrl = 'https://api.github.com/repos/Nonsultant/specgurka/releases/latest';

    try {
        const [repoResponse, releaseResponse] = await Promise.all([
            fetch(repoUrl),
            fetch(latestReleaseUrl)
        ]);

        const repoData = await repoResponse.json();
        const releaseData = await releaseResponse.json();

        document.getElementById('latest-version').textContent = releaseData.tag_name || 'No releases';
        document.getElementById('stars-count').textContent = repoData.stargazers_count;
        document.getElementById('forks-count').textContent = repoData.forks_count;
    } catch (error) {
        console.error('Error fetching GitHub data:', error);
        document.getElementById('latest-version').textContent = 'Error';
        document.getElementById('stars-count').textContent = 'Error';
        document.getElementById('forks-count').textContent = 'Error';
    }
});