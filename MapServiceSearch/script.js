var MapServiceSearchApp = angular.module("MapServiceSearchApp", ['elasticsearch']);

MapServiceSearchApp.service("client", function (esFactory) {
    return esFactory({
        host: "https://mapServiceSearchReadOnly:lgtaluq539xm3zoseiyalk2kfdndjrat@gloin-eu-west-1.searchly.com"
    });
});

MapServiceSearchApp.controller("MapServiceSearchCtrl", function ($scope, client) {
    $scope.mapServices = [];
    $scope.page = 0;
    $scope.totalNumberOfResults = 0;
    $scope.pageSize = 10;
    $scope.allResults = true;

    // the following lines should check whether Elasticsearch is available.
    // Unfortunately, this doesn't work with the Elasticsearch client object and Searchly.
    // When using this app in an on premise environment, you can safely uncomment the code and
    // remove the trailing '$scope.searchAvailable = true;'
    //client.cluster.state({
    //    metric: [
    //      "cluster_name",
    //      "nodes",
    //      "master_node",
    //      "version"
    //    ]
    //})
    //.then(function () {
    //    $scope.searchAvailable = true;
    //    $scope.error = null;
    //})
    //.catch(function (err) {
    //    $scope.searchAvailable = false;
    //    $scope.error = err;
    //});
    $scope.searchAvailable = true;

    client.search({
        index: "mapservicesearch",
        type: "mss",
        body: {
            aggregations: {
                environments: {
                    terms: {
                        field: "serverEnvironment"
                    }
                    
                },
                types: {
                    terms: {
                        field: "serverType"
                    }
                }
            }
        }
    }).then(function (result) {
        var i;

        $scope.serverTypes = [""];
        for (i = 0; i < result.aggregations.types.buckets.length; i++) {
            var currentType = result.aggregations.types.buckets[i];
            $scope.serverTypes.push(currentType.key.toUpperCase());
        }
        $scope.selectedServerType = $scope.serverTypes[0];

        $scope.serverEnvironments = [""];
        for (i = 0; i < result.aggregations.environments.buckets.length; i++) {
            var currentEnvironment = result.aggregations.environments.buckets[i];
            $scope.serverEnvironments.push(currentEnvironment.key.toUpperCase());
        }
        $scope.selectedServerEnvironment = $scope.serverEnvironments[0];
    }, function (error) {
        $scope.searchAvailable = false;
        $scope.error = error.message;
    });

    $scope.search = function () {
        $scope.mapServices = [];
        $scope.page = 0;
        $scope.totalNumberOfResults = 0;
        $scope.allResults = true;
        $scope.selectedMapService = null;
        $scope.innerSearch(0);
    };

    $scope.innerSearch = function (offset) {
        var searchTerm = $scope.searchTerm;
        if (!searchTerm || searchTerm.length < 3) {
            $scope.error = new Error("Enter a search term with at least 3 characters");

            return;
        } else {
            $scope.error = null;
        }

        var query = "*" + searchTerm + "*";
        if ($scope.selectedServerType) {
            query += " AND serverType:" + $scope.selectedServerType;
        }

        if ($scope.selectedServerEnvironment) {
            query += " AND serverEnvironment:" + $scope.selectedServerEnvironment;
        }

        client.search({
            index: "mapservicesearch",
            type: "mss",
            q: query,
            size: $scope.pageSize,
            from : offset
        }).then(function (result) {
            var ii = 0;
            var hitsIn = (result.hits || {}).hits || [];
            var retrievedNumberOfResults = hitsIn.length;

            if (retrievedNumberOfResults > 0) {
                for (; ii < retrievedNumberOfResults; ii++) {
                    $scope.mapServices.push(hitsIn[ii]._source);
                }

                var totalNumberOfResults = result.hits.total;
                $scope.totalNumberOfResults = totalNumberOfResults;

                if (totalNumberOfResults > ($scope.pageSize + offset)) {
                    $scope.allResults = false;
                } else {
                    $scope.allResults = true;
                }
            }
        }, function (error) {
            console.trace(error.message);
        });
    };

    $scope.loadMore = function () {
        if ($scope.allResults === false) {
            $scope.page++;
            var offset = $scope.page * $scope.pageSize;
            $scope.innerSearch(offset);
        }
    };
    
    $scope.showModal = false;

    $scope.toggleModal = function (mapServiceSelected) {
        // iterate over all datasources and their datasources and mark the
        // datasources which contain the search term. Thus, they can be 
        // highlighted in the HTML.
        var numberOfDatasets = mapServiceSelected.datasources.length;
        var searchTermLowerCase = $scope.searchTerm.toLowerCase();
        for (var i = (numberOfDatasets - 1) ; i >= 0; i--) {
            var currentDatasource = mapServiceSelected.datasources[i];
            currentDatasource.containsSearchTerm = false;

            // iterate over all attributes of the current datasource
            for (var property in currentDatasource) {
                if (currentDatasource.hasOwnProperty(property)) {
                    var currentAttribute = currentDatasource[property];
                    if (!currentAttribute) {
                        continue;
                    }

                    // do a lower-case comparison and break the loop in case we found a match
                    var currentAttributeToLowerCase = currentAttribute.toLowerCase();
                    var containsSeachTerm = currentAttributeToLowerCase.indexOf(searchTermLowerCase) !== -1;

                    if (containsSeachTerm) {
                        currentDatasource.containsSearchTerm = true;
                        break;
                    }
                }
            }
        }

        $scope.selectedMapService = mapServiceSelected;
        $scope.showModal = !$scope.showModal;
    };

});

MapServiceSearchApp.directive("modal", function () {
    return {
        template: '<div class="modal fade bd-example-modal-lg">' +
            '<div class="modal-dialog modal-lg">' +
            '<div class="modal-content">' +
            '<div class="modal-header">' +
            '<button type="button" class="close" data-dismiss="modal" aria-hidden="true">&times;</button>' +
            '<h4 class="modal-title">{{ title }}</h4>' +
            "</div>" +
            '<div class="modal-body" ng-transclude></div>' +
            "</div>" +
            "</div>" +
            "</div>",
        restrict: "E",
        transclude: true,
        replace: true,
        scope: true,
        link: function(scope, element, attrs) {
            scope.title = attrs.title;

            scope.$watch(attrs.visible, function (value) {
                if (value === true)
                    window.$(element).modal("show");
                else
                    window.$(element).modal("hide");
            });

            window.$(element).on("shown.bs.modal", function () {
                scope.$apply(function () {
                    scope.$parent[attrs.visible] = true;
                });
            });

            window.$(element).on("hidden.bs.modal", function () {
                scope.$apply(function () {
                    scope.$parent[attrs.visible] = false;
                });
            });
        }
    };
});