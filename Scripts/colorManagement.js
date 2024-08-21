$(document).ready(function () {
    var defaultColor = "#808080";

    // Configuración del color picker para la barra de navegación
    $("#navbarColorPicker").spectrum({
        color: defaultColor,
        showInput: true,
        preferredFormat: "hex",
        showPalette: true,
        palette: [
            ["#007BFF", "#FF0000", "#86B038"]
        ],
        change: function (color) {
            var selectedColor = color.toHexString();
            document.getElementById('navbar').style.backgroundColor = selectedColor;
            localStorage.setItem('navbarColor', selectedColor);
        }
    });

    // Configuración del color picker para el slider
    $("#sliderColorPicker").spectrum({
        color: defaultColor,
        showInput: true,
        preferredFormat: "hex",
        showPalette: true,
        palette: [
            ["#007BFF", "#FF0000", "#d6f792"]
        ],
        change: function (color) {
            var selectedColor = color.toHexString();
            document.querySelector('.sb-sidenav-menu').style.backgroundColor = selectedColor;
            localStorage.setItem('sliderColor', selectedColor);
        }
    });

    // Aplicar colores guardados o gris predeterminado
    var savedNavbarColor = localStorage.getItem('navbarColor') || defaultColor;
    document.getElementById('navbar').style.backgroundColor = savedNavbarColor;
    $("#navbarColorPicker").spectrum("set", savedNavbarColor);

    var savedSliderColor = localStorage.getItem('sliderColor') || defaultColor;
    document.querySelector('.sb-sidenav-menu').style.backgroundColor = savedSliderColor;
    $("#sliderColorPicker").spectrum("set", savedSliderColor);
});
