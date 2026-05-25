// Simple carousel: inject slides, autoplay, dots and manual select
document.addEventListener('DOMContentLoaded', function(){
  const images = [
    {src: 'https://images.unsplash.com/photo-1509395176047-4a66953fd231?auto=format&fit=crop&w=1400&q=80', alt: 'Chantier de gros oeuvre avec échafaudages et fondations', caption: 'Chantier robuste et optimisation des étapes de fondation'},
    {src: 'https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?auto=format&fit=crop&w=1400&q=80', alt: 'Équipe d’ouvriers de construction en action', caption: 'Équipes qualifiées mobilisées pour votre projet au Cameroun'},
    {src: 'https://images.unsplash.com/photo-1526318472351-c75fcf0706e2?auto=format&fit=crop&w=1400&q=80', alt: 'Grue sur site de construction urbain', caption: 'Suivi de chantier précis sur les grandes structures'},
    {src: 'https://images.unsplash.com/photo-1560184897-e2b8a5a8c8c6?auto=format&fit=crop&w=1400&q=80', alt: 'Chargement de matériaux et approvisionnement', caption: 'Logistique et approvisionnement maîtrisés pour chaque chantier'},
    {src: 'https://images.unsplash.com/photo-1494526585095-c41746248156?auto=format&fit=crop&w=1400&q=80', alt: 'Architecte et plans de construction', caption: 'Planification rigoureuse et documents clairs pour la diaspora'},
    {src: 'https://images.unsplash.com/photo-1560250097-0b93528c311a?auto=format&fit=crop&w=1400&q=80', alt: 'Bâtiment en construction avec échafaudage', caption: 'Visibilité totale sur l’avancement de vos projets de construction'}
  ];

  const track = document.getElementById('carousel-track');
  const dotsWrap = document.getElementById('carousel-dots');
  const captionEl = document.getElementById('carousel-caption');
  const prevBtn = document.querySelector('.carousel-prev');
  const nextBtn = document.querySelector('.carousel-next');
  if(!track || !dotsWrap || !captionEl) return;

  images.forEach((img,i)=>{
    const slide = document.createElement('div');
    slide.className = 'carousel-slide';
    const im = document.createElement('img');
    im.src = img.src;
    im.alt = img.alt;
    im.onerror = () => {
      im.classList.add('error');
      slide.setAttribute('aria-label', 'Image non disponible, fond de secours affiché');
    };
    slide.appendChild(im);
    track.appendChild(slide);

    const dot = document.createElement('div');
    dot.className = 'hero-dot' + (i===0? ' active':'');
    dot.dataset.index = i;
    dot.addEventListener('click', ()=> goTo(i));
    dotsWrap.appendChild(dot);
  });

  const slides = track.children;
  let index = 0;
  let timer = null;

  function update(){
    track.style.transform = `translateX(-${index * 100}%)`;
    Array.from(dotsWrap.children).forEach((d, i)=> d.classList.toggle('active', i===index));
    captionEl.textContent = images[index].caption || images[index].alt;
  }

  function goTo(i){
    index = (i + slides.length) % slides.length;
    update();
    resetTimer();
  }

  function next(){ goTo(index+1); }

  prevBtn?.addEventListener('click', ()=> goTo(index-1));
  nextBtn?.addEventListener('click', ()=> goTo(index+1));

  function resetTimer(){
    if(timer) clearInterval(timer);
    timer = setInterval(next, 4000);
  }

  update();
  resetTimer();
});
