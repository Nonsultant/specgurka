async function fetchRandomCatImage(imgId) {
    try {
      const response = await fetch('https://api.thecatapi.com/v1/images/search');
      const data = await response.json();
      const imageUrl = data[0].url;
      document.getElementById(imgId).src = imageUrl;
    } catch (error) {
      console.error('Error fetching random cat image:', error);
    }
  }

  // Fetch a random cat image for each link when the page loads
  window.onload = function () {
    const links = document.querySelectorAll('.link_img');
    links.forEach(link => {
      fetchRandomCatImage(link.id);
    });
  };