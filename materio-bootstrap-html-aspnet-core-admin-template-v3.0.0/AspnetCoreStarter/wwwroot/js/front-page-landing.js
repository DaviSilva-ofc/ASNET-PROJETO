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
    // determine number of slides dynamically so loopedSlides stays in sync when we add/remove reviews
    const reviewCount = swiperReviews.querySelectorAll('.swiper-slide').length;
    console.log('initial reviewCount for swiper:', reviewCount);
    const swiperInstance = new Swiper(swiperReviews, {
      // always show single testimonial at once
      slidesPerView: 1,
      spaceBetween: 5,
      grabCursor: true,
      autoplay: {
        delay: 3000,
        disableOnInteraction: false
      },
      speed: 500,
      // no loop — we'll restart manually to keep pagination sane
      loop: false,
      pagination: {
        el: '.swiper-pagination',
        clickable: true,
        dynamicBullets: true
      },
      observer: true,
      observeParents: true,
      breakpoints: {
        768: {
          slidesPerView: 1,
          spaceBetween: 20
        }
      },
      on: {
        init: function() {
          console.log('swiper init: total slides =', this.slides.length);
          this.slides.forEach((slide, idx) => {
            const img = slide.querySelector('img.client-logo');
            console.log('  slide[' + idx + '] src', img ? img.src : 'none');
          });
        },
        slideChange: function() {
          console.log('swiper slideChange: activeIndex', this.activeIndex, 'realIndex', this.realIndex);
          if (this.activeIndex === reviewCount - 1) {
            // reached final testimonial -> immediately jump back to start
            // use zero duration so pagination resets in sync
            this.slideTo(0, 0);
            // ensure pagination bullets updated correctly
            if (this.pagination && this.pagination.update) {
              this.pagination.update();
            }
            // restart autoplay cycle explicitly
            if (this.autoplay && this.autoplay.start) {
              this.autoplay.start();
            }
          }
        }
      }
    });
  }

  // Review client logo
  // -----------------------------------
  if (swiperLogos) {
    new Swiper(swiperLogos, {
      slidesPerView: 2,
      autoplay: {
        delay: 3000,
        disableOnInteraction: false
      },
      breakpoints: {
        992: {
          slidesPerView: 5
        },
        768: {
          slidesPerView: 3
        }
      }
    });
  }
})();
