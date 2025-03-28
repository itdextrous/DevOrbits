
// Navigation ACTIVE class
$(document).ready(function(){
	$('.sidebar li a[href="' + document.location.pathname + '"]').parent().addClass('active');
});


// Navbar Toggler
$('.navbar-toggler').click(function(){
  $('body').toggleClass('nav-open');
});

$('.close-layer').click(function(){
  $('body').removeClass('nav-open');
});

$(document).ready(function () {
	$('[data-toggle="popover"]').popover()
  	$('[data-toggle="tooltip"]').tooltip()
});