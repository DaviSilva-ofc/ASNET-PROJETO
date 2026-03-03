/**
 * Main - Front Pages
 */
'use strict';

(function () {
  const sliderPricing = document.getElementById('slider-pricing'),
    swiperLogos = document.getElementById('swiper-clients-logos'),
    swiperReviews = document.getElementById('swiper-reviews');

  // Hero
  const mediaQueryXL = '1200';
  const width = screen.width;
  if (width >= mediaQueryXL) {
    document.addEventListener('mousemove', function parallax(e) {
      this.querySelectorAll('.animation-img').forEach(layer => {
        let speed = layer.getAttribute('data-speed');
        let x = (window.innerWidth - e.pageX * speed) / 100;
        let y = (window.innerWidth - e.pageY * speed) / 100;
        layer.style.transform = `translate(${x}px, ${y}px)`;
      });
    });
  }

  // noUiSlider
  // Pricing slider
  // -----------------------------------
  if (sliderPricing) {
    noUiSlider.create(sliderPricing, {
      start: [458],
      step: 1,
      connect: [true, false],
      behaviour: 'tap-drag',
      direction: isRtl ? 'rtl' : 'ltr',
      tooltips: [
        {
          to: function (value) {
            return parseFloat(value).toLocaleString('en-EN', { minimumFractionDigits: 0 }) + '+';
          }
        }
      ],
      range: {
        min: 0,
        max: 916
      }
    });
  }

  // swiper carousel
  // Customers reviews
  // -----------------------------------
  if (swiperReviews) {
    new Swiper(swiperReviews, {
      // Mostra 1 slide inteiro ao centro com meio slide de cada lado
      slidesPerView: 1.5,
      centeredSlides: true,
      spaceBetween: 32,
      grabCursor: true,
      // Loop nativo: quando chega ao último, o primeiro aparece do lado direito
      loop: true,
      autoplay: {
        delay: 3000,
        disableOnInteraction: false
      },
      speed: 600,
      // Paginação com bolinhas correspondentes aos slides reais (sem contar duplicados do loop)
      pagination: {
        el: '.swiper-pagination',
        clickable: true,
        dynamicBullets: false
      },
      observer: true,
      observeParents: true,
      breakpoints: {
        // Em ecrãs pequenos mostra apenas 1 slide completo
        0: {
          slidesPerView: 1,
          centeredSlides: false,
          spaceBetween: 16
        },
        // A partir de 768px mostra 1.5
        768: {
          slidesPerView: 1.5,
          centeredSlides: true,
          spaceBetween: 32
        }
      }
    });
  }

  // Review client logo
  // -----------------------------------
  if (swiperLogos) {
    new Swiper(swiperLogos, {
      slidesPerView: 2,
      spaceBetween: 100,
      loop: true,
      autoplay: {
        delay: 3000,
        disableOnInteraction: false
      },
      breakpoints: {
        992: {
          slidesPerView: 5,
          spaceBetween: 120
        },
        768: {
          slidesPerView: 3,
          spaceBetween: 80
        }
      }
    });
  }
})();
