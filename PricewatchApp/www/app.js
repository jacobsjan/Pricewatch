(function () {
    'use strict';
    var module = angular.module('app', ['onsen']);

    function parsePrice(price) {
        var f = parseFloat(price.replace(',', '.'));
        return isNaN(f) ? 0 : f;
    }

    module.controller('AddController', function ($scope, $data, $http) {
        $scope.addPricewatch = function () {
            // Show loading pop-up
            modal.show();

            // Encode the url parameter twice otherwise the Azure function's routing goes haywire
            var url = encodeURIComponent(encodeURIComponent($scope.url));

            $data.addPricewatch(url, $http).catch(function (response) {
                modal.hide(); // Hide loading screen
                alert("Failed to add watch.");
            }).then(function (response) {
                $data.pricewatches.push(response.data);
                $data.pricewatches.sort(function (a, b) { return a.Name.localeCompare(b.Name) });
                $data.selectedItem = response.data;
                modal.hide(); // Hide loading screen
                myNavigator.replacePage('detail.html');
            });
        };

        $scope.urlChanged = function () {
            var addButtonElement = document.getElementById("add-button");
            if ($scope.url.toLowerCase().startsWith("https://")) {
                if (addButtonElement.hasAttribute("disabled")) addButtonElement.removeAttribute("disabled");
            } else {
                if (!addButtonElement.hasAttribute("disabled")) addButtonElement.disabled = "true";
            }
        }
    });
    
    module.controller('DetailController', function ($scope, $data, $http) {
        $scope.item = $data.selectedItem;

        $data.getPriceHistory($data.selectedItem, $http).then(function (response) {
            for (var i = 0; i < response.data.length; i++) {
                var price = response.data[i];
                price.Date = new Date(Date.parse(price.Date)).toLocaleDateString("nl-be");

                if (i < response.data.length - 1) {
                    var nextPrice = response.data[i + 1];
                    price.Color = parsePrice(price.Price) < parsePrice(nextPrice.Price) ? "darkgreen" : "darkred";
                } else {
                    price.Color = "black";
                }
            }

            $scope.pricehistory = response.data;
        });

        $scope.requestDelete = function () {
            ons.notification.confirm({
                message: "Are you sure you want to stop watching '" + $scope.item.Name + "'?",
                callback: function (answer) {
                    if (answer == 1) { // 'OK'
                        // Show loading pop-up
                        modal.show();
                        $data.deletePricewatch($scope.item, $http).catch(function (response) {
                            modal.hide(); // Hide loading screen
                            alert("Failed to delete watch.");
                        }).then(function (response) {
                            // Remove local copy of price watch
                            $data.pricewatches.splice($data.pricewatches.indexOf($scope.item), 1);

                            modal.hide(); // Hide loading screen
                            myNavigator.popPage();
                        });
                    } 
                }
            });
        }
    });

    module.controller('HomeController', function ($scope, $data, $http) {
        // Load pricewatch data
        $scope.isLoading = true;
        $data.getPricewatches($http).then(function (response) {
            angular.forEach(response.data, function (pricewatch) {
                // Add a LowerPrice property to indicate the previous price of the app if it's lower than the current one
                pricewatch["LowerPrice"] = pricewatch.PreviousPrice && (parsePrice(pricewatch.Price) < parsePrice(pricewatch.PreviousPrice)) ? pricewatch.PreviousPrice : null;

                // Load the real image url's from the server
                $data.getImageUrl(pricewatch, $http).then(function (imgResponse) {
                    pricewatch.ImageUrl = imgResponse.data;
                });
            });

            $scope.isLoading = false;
            $data.pricewatches = $scope.pricewatches = response.data;
        });

        $scope.showDetail = function (index) {
            var selectedItem = $data.pricewatches[index];
            $data.selectedItem = selectedItem;
            myNavigator.pushPage('detail.html');
        };

        $scope.showAdd = function (index) {
            myNavigator.pushPage('add.html');
        };
    });

    module.factory('$data', function () {
        var data = {};

        data.getPricewatches = function($http) {
            return $http.get("/api/GetPricewatches/");
        };

        data.getImageUrl = function(pricewatch, $http) {
            return $http.get("/api/GetAppImageUrl/appName/" + pricewatch.Name);
        };

        data.getPriceHistory = function (pricewatch, $http) {
            return $http.get("/api/GetPriceHistory/appName/" + pricewatch.Name);
        }

        data.addPricewatch = function (url, $http) {
            return $http.get("/api/AddPricewatch/url/" + url);
        }

        data.deletePricewatch = function (pricewatch, $http) {
            return $http.get("/api/DeletePricewatch/appName/" + pricewatch.Name);
        };

        return data;
    });
})();

