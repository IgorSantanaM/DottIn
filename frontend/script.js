// ===== DottIn Landing Page — Scripts =====

// ----- STRIPE CONFIG -----
const API_BASE_URL = 'http://localhost:5000'; // Change to production URL when deploying
const STRIPE_PUBLISHABLE_KEY = 'pk_test_51TJvpW0mdZxVzzZMSOswy0dBL977nTs6DsRU6Mu6eBB6hrvL2u2FZggidVFQ6U0sXsEOPKhg40ntk9iWPembjOE900ClO93yKa';

document.addEventListener('DOMContentLoaded', () => {

  // ----- STRIPE CHECKOUT -----
  let stripe = null;

  // Initialize Stripe with publishable key
  async function initStripe() {
    try {
      // Try to get from backend first, fallback to hardcoded key
      try {
        const response = await fetch(`${API_BASE_URL}/api/billing/config`);
        if (response.ok) {
          const config = await response.json();
          stripe = Stripe(config.publishableKey);
          return;
        }
      } catch (e) {
        console.log('Backend not available, using hardcoded Stripe key');
      }
      // Fallback to hardcoded key for testing
      stripe = Stripe(STRIPE_PUBLISHABLE_KEY);
    } catch (error) {
      console.error('Failed to initialize Stripe:', error);
    }
  }

  // Handle checkout button clicks
  async function handleCheckout(priceId, planName) {
    if (!stripe) {
      alert('Sistema de pagamento não disponível. Tente novamente.');
      return;
    }

    const button = document.querySelector(`[data-price-id="${priceId}"]`);
    const originalText = button.textContent;
    button.textContent = 'Carregando...';
    button.disabled = true;

    try {
      // Get auth token from localStorage (user must be logged in)
      const token = localStorage.getItem('authToken');
      if (!token) {
        alert('Você precisa estar logado para assinar um plano.');
        window.location.href = '/login'; // Adjust to your login page
        return;
      }

      const response = await fetch(`${API_BASE_URL}/api/billing/checkout-session`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({ priceId })
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Erro ao criar sessão de checkout');
      }

      const session = await response.json();
      
      // Redirect to Stripe Checkout
      const result = await stripe.redirectToCheckout({
        sessionId: session.sessionId
      });

      if (result.error) {
        throw new Error(result.error.message);
      }
    } catch (error) {
      console.error('Checkout error:', error);
      alert(`Erro no checkout: ${error.message}`);
    } finally {
      button.textContent = originalText;
      button.disabled = false;
    }
  }

  // Attach click handlers to pricing buttons
  document.querySelectorAll('[data-price-id]').forEach(button => {
    button.addEventListener('click', (e) => {
      e.preventDefault();
      const priceId = button.dataset.priceId;
      const planName = button.dataset.plan;
      handleCheckout(priceId, planName);
    });
  });

  // Initialize Stripe on page load
  initStripe();

  // ----- NAVBAR SCROLL EFFECT -----
  const navbar = document.getElementById('navbar');
  const onScroll = () => {
    navbar.classList.toggle('scrolled', window.scrollY > 60);
  };
  window.addEventListener('scroll', onScroll, { passive: true });
  onScroll();

  // ----- MOBILE MENU TOGGLE -----
  const mobileToggle = document.getElementById('mobileToggle');
  const navLinks = document.getElementById('navLinks');

  mobileToggle.addEventListener('click', () => {
    mobileToggle.classList.toggle('active');
    navLinks.classList.toggle('open');
  });

  // Close mobile menu on link click
  navLinks.querySelectorAll('a').forEach(link => {
    link.addEventListener('click', () => {
      mobileToggle.classList.remove('active');
      navLinks.classList.remove('open');
    });
  });

  // ----- INTERSECTION OBSERVER — SCROLL REVEAL -----
  const revealElements = document.querySelectorAll('.reveal');

  const revealObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        entry.target.classList.add('visible');
        revealObserver.unobserve(entry.target);
      }
    });
  }, {
    threshold: 0.12,
    rootMargin: '0px 0px -40px 0px'
  });

  revealElements.forEach(el => revealObserver.observe(el));

  // ----- COUNTER ANIMATION -----
  const statNumbers = document.querySelectorAll('.stat-number[data-target]');

  const counterObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        animateCounter(entry.target);
        counterObserver.unobserve(entry.target);
      }
    });
  }, { threshold: 0.5 });

  statNumbers.forEach(el => counterObserver.observe(el));

  function animateCounter(el) {
    const target = parseFloat(el.dataset.target);
    const suffix = el.dataset.suffix || '';
    const isDecimal = el.dataset.decimal === 'true';
    const duration = 2000;
    const startTime = performance.now();

    function update(currentTime) {
      const elapsed = currentTime - startTime;
      const progress = Math.min(elapsed / duration, 1);

      // Ease out cubic
      const eased = 1 - Math.pow(1 - progress, 3);
      const current = eased * target;

      if (isDecimal) {
        el.textContent = current.toFixed(1) + suffix;
      } else {
        el.textContent = Math.floor(current).toLocaleString('pt-BR') + suffix;
      }

      if (progress < 1) {
        requestAnimationFrame(update);
      }
    }

    requestAnimationFrame(update);
  }

  // ----- TESTIMONIAL CAROUSEL -----
  const track = document.getElementById('testimonialsTrack');
  const dots = document.querySelectorAll('.testimonial-dot');
  let currentSlide = 0;
  const totalSlides = dots.length;
  let autoPlayTimer;

  function goToSlide(index) {
    currentSlide = index;
    track.style.transform = `translateX(-${index * 100}%)`;
    dots.forEach((dot, i) => {
      dot.classList.toggle('active', i === index);
    });
  }

  dots.forEach(dot => {
    dot.addEventListener('click', () => {
      goToSlide(parseInt(dot.dataset.index));
      resetAutoPlay();
    });
  });

  function autoPlay() {
    autoPlayTimer = setInterval(() => {
      goToSlide((currentSlide + 1) % totalSlides);
    }, 5000);
  }

  function resetAutoPlay() {
    clearInterval(autoPlayTimer);
    autoPlay();
  }

  autoPlay();

  // ----- SMOOTH SCROLL FOR ALL ANCHOR LINKS -----
  document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
      const target = document.querySelector(this.getAttribute('href'));
      if (target) {
        e.preventDefault();
        const offsetTop = target.getBoundingClientRect().top + window.pageYOffset - 80;
        window.scrollTo({ top: offsetTop, behavior: 'smooth' });
      }
    });
  });

  // ----- NAVBAR ACTIVE LINK HIGHLIGHT -----
  const sections = document.querySelectorAll('section[id]');

  const sectionObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        const id = entry.target.getAttribute('id');
        document.querySelectorAll('.nav-links a').forEach(link => {
          link.style.color = '';
          if (link.getAttribute('href') === `#${id}`) {
            link.style.color = 'var(--text-primary)';
          }
        });
      }
    });
  }, {
    threshold: 0.3,
    rootMargin: '-80px 0px -50% 0px'
  });

  sections.forEach(section => sectionObserver.observe(section));

});
