(function($){
  // Small helper to toggle show class
  function setOpen($el, open) {
    if (open) {
      $el.addClass('show');
      $el.children('.dropdown-menu').addClass('show');
    } else {
      $el.removeClass('show');
      $el.children('.dropdown-menu').removeClass('show');
    }
  }

  var hoverDelay = 160;
  var closeDelay = 220;
  var timers = new WeakMap();

  $(document).on('mouseenter', '.nav-item, .dropdown-submenu', function(){
    if (window.innerWidth <= 991) return; // ignore on mobile
    var $this = $(this);
    clearTimeout(timers.get(this));
    var t = setTimeout(function(){ setOpen($this, true); }, hoverDelay);
    timers.set(this, t);
  });

  $(document).on('mouseleave', '.nav-item, .dropdown-submenu', function(){
    if (window.innerWidth <= 991) return;
    var $this = $(this);
    clearTimeout(timers.get(this));
    var t = setTimeout(function(){ setOpen($this, false); }, closeDelay);
    timers.set(this, t);
  });

  // Mobile: click to toggle
  $(document).on('click', '.nav-item > .nav-link, .dropdown-submenu > a', function(e){
    if (window.innerWidth > 991) return; // desktop keep default
    var $parent = $(this).closest('li');
    e.preventDefault();
    var isOpen = $parent.hasClass('show');
    $parent.siblings('.show').each(function(){ setOpen($(this), false); });
    setOpen($parent, !isOpen);
  });

  // close on outside click (desktop)
  $(document).on('click', function(e){
    if (window.innerWidth <= 991) return;
    if ($(e.target).closest('.nav-item, .dropdown-menu').length === 0) {
      $('.nav-item.show, .dropdown-submenu.show').each(function(){ setOpen($(this), false); });
    }
  });

  // Accessibility: Esc to close
  $(document).on('keydown', function(e){ if (e.key === 'Escape') $('.nav-item.show, .dropdown-submenu.show').each(function(){ setOpen($(this), false); }); });
})(jQuery);