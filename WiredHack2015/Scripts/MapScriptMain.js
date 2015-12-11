var map, pointarray, heatmap;
var bool = true;

function changeGradient() {
    var gradient = [
        "rgba(0, 255, 255, 0)",
        "rgba(0, 255, 255, 1)",
        "rgba(0, 191, 255, 1)",
        "rgba(0, 127, 255, 1)",
        "rgba(0, 63, 255, 1)",
        "rgba(0, 0, 255, 1)",
        "rgba(0, 0, 223, 1)",
        "rgba(0, 0, 191, 1)",
        "rgba(0, 0, 159, 1)",
        "rgba(0, 0, 127, 1)",
        "rgba(63, 0, 91, 1)",
        "rgba(127, 0, 63, 1)",
        "rgba(191, 0, 31, 1)",
        "rgba(255, 0, 0, 1)"
    ];
    heatmap.set("gradient", heatmap.get("gradient") ? null : gradient);
}

function changeRadius() {
    heatmap.set("radius", heatmap.get("radius") ? null : 40);
}

function toggleHeatmap() {
    heatmap.setMap(heatmap.getMap() ? null : map);

}

//google.maps.event.addDomListener(window, 'load', initialize);
function initialize() {
    var bounds = new google.maps.LatLngBounds();
    var mapOptions;
    if (Templat != "" & Templong != "") {
        mapOptions = {
            zoom: Zoomin,
            center: new google.maps.LatLng(Templat, Templong),
            mapTypeId: google.maps.MapTypeId.ROADMAP
        };
    } else {
        mapOptions = {
            zoom: Zoomin,
            center: new google.maps.LatLng(38.5000, -98.0000),
            mapTypeId: google.maps.MapTypeId.ROADMAP
        };
    }

    map = new google.maps.Map(document.getElementById('map-canvas'),
          mapOptions);

    var infoWindow = new google.maps.InfoWindow(), marker, i;
    var ourMarkers = [];
    function setMap(map) {
        for (var i = 0; i < ourMarkers.Length; i++) {
            ourMarkers[i].setMap(map);
        }
    }
    function hideMarkers() {
        setMap(null);
    }

    function showMarkers() {
        setMap(map);
    }


    for (i = 0; i < markers.length; i++) {
        var position = new google.maps.LatLng(markers[i][1], markers[i][2]);
        bounds.extend(position);
        marker = new google.maps.Marker({
            position: position,
            map: map,
            title: markers[i][0]
        });
        ourMarkers.push(marker);
        // Allow each marker to have an info window    
        google.maps.event.addListener(marker, 'click', (function (marker, i) {
            return function () {
                infoWindow.setContent(infoWindowContent[i][0]);
                infoWindow.open(map, marker);
            }
        })(marker, i));

        google.maps.event.addListener(map, 'zoom_changed', function () {
            if (map.getZoom() <= 8) {
                hideMarkers();

            } else {
                showMarkers();
            }
        });

        // Automatically center the map fitting all markers on the screen
        map.fitBounds(bounds);
    }





    var boundsListener = google.maps.event.addListener((map), 'bounds_changed', function () {
        map.setZoom(Zoomin);
        google.maps.event.removeListener(boundsListener);
    });

    var pointArray = new google.maps.MVCArray(heatmapData);

    heatmap = new google.maps.visualization.HeatmapLayer({
        data: pointArray
    });
    heatmap.setMap(map);
}