/// <reference path="libraries/jquery.js" />
/// <reference path="libraries/knockout.js" />
/// <reference path="CrmWebApiFacade.js" />
(function ($, jQuery, facade) {
  window.AdvancedMultiSelectControl = function () {
    var page = window.parent.Xrm.Page;
    var formType = {
      FORM_TYPE_CREATE: 1,
      FORM_TYPE_UPDATE: 2,
      FORM_TYPE_READ_ONLY: 3,
      FORM_TYPE_DISABLED: 4,
      FORM_TYPE_BULK_EDIT: 6
    };

    function lockBackspace() {
      $(window.document).unbind('keydown').bind('keydown', function (event) {
        if (event.keyCode === 8) {
          event.preventDefault();
        }
      });
    }

    function getWebResourceParam(name) {
      if (location.search !== "") {
        var vals = location.search.substr(1).split("&");
        for (var i in vals) {
          if (vals.hasOwnProperty(i)) {
            vals[i] = vals[i].replace(/\+/g, " ").split("=");
          }
        }

        for (var j in vals) {
          if (vals.hasOwnProperty(j)) {
            if (vals[j][0].toLowerCase() === name.toLowerCase()) {
              return vals[j][1];
            }
          }
        }
      }

      return null;
    }

    function getWebResourceDataParams() {
      var dataParams = getWebResourceParam("data");
      if (dataParams == null) {
        return null;
      }

      var result = {};

      var vals = decodeURIComponent(dataParams).split("&");
      for (var i in vals) {
        if (vals.hasOwnProperty(i)) {
          var pair = vals[i].replace(/\+/g, " ").split("=");
          if (pair[0]) {
            var key = pair[0].trim().toLowerCase();
            result[key] = pair[1];
          }
        }
      }

      return result;
    }

    var viewModel = function (columnCount) {
      var self = this;
      self.DataLoaded = ko.observable(false);
      self.ItemSet = ko.observableArray([]);
      self.ItemSetSelected = ko.observableArray([]);
      self.ItemSetByRows = ko.observableArray([]);
      self.ColumnCount = ko.observable((parseInt(columnCount) || 1));
      self.SavingAttr = null;
      self.Items = null;
      self.ErrorMessage = ko.observable(null);
      self.ModeMessage = ko.observable(null);
      self.AllowUpdate = ko.observable(false);
      self.ReadOnlyMode = ko.observable(true);
      self.Visible = ko.observable(true);
      self.SelectizeMode = ko.observable(false);

      //self.Test1 = function() {
      //  debugger;
      //}

      var buildValueForSavingAttr = function () {
        var checkedSet = [];
        var arr, i, max;
        if (self.SelectizeMode()) {
          arr = self.ItemSetSelected();
          for (i = 0, max = arr.length; i < max; i++) {
            checkedSet.push(arr[i]);
          }
        } else {
          arr = self.ItemSet();
          for (i = 0, max = arr.length; i < max; i++) {
            if (arr[i].Value()) {
              checkedSet.push(arr[i].Id);
            }
          }
        }

        return checkedSet.length === 0 ? "-" : checkedSet.join(';');
      };

      var onSelectedChanged = function () {
        var value = buildValueForSavingAttr();
        self.SavingAttr.setSubmitMode("always");
        self.SavingAttr.setValue(value);
      };

      var getCurrentSavingAttrValueSet = function () {
        var idsString = self.SavingAttr.getValue();
        if (idsString == null || idsString === "") {
          return null;
        }

        return idsString === "-" ? [] : idsString.split(";");
      }

      var getItems = function (entityId, entityName, itemSetName) {
        var dfd = $.Deferred();
        var dataParams = {
          "EntityLogicalName": entityName,
          "RecordId": entityId,
          "ItemSetName": itemSetName
        };

        facade.ExecuteAction("pavelkh_GetItemSet", dataParams).then(function (response) {
          self.SavingAttr = page.getAttribute(response.SavingAttributeLogicalName);
          if (!self.SavingAttr) {
            dfd.reject(response.SavingAttributeLogicalName + " hidden dummy saving attribute is expected to be on the form but is not found on it.");
          }

          self.Items = JSON.parse(response.Items);
          self.AllowUpdate(JSON.parse(response.AllowUpdate));
          dfd.resolve();
        },
          dfd.reject);

        return dfd.promise();
      };

      var getData = function() {
        var dfd = $.Deferred();

        var entityName = getWebResourceParam('typename');
        if (!entityName) {
          dfd.reject({ message: 'Error! Entity type is not provided to the checkbox set control.' });
          return dfd.promise();
        }

        var id = decodeURIComponent(getWebResourceParam("id"));
        var dataParams = getWebResourceDataParams();
        if (!dataParams) {
          dfd.reject({ message: 'Error! Data parameters is not provided to the checkbox set control.' });
          return dfd.promise();
        }

        var itemSetName = dataParams['itemsetname'];
        if (!itemSetName) {
          dfd.reject({ message: 'Error! Data parameters provided to the multiselect set control are invalid.' });
          return dfd.promise();
        }

        itemSetName = itemSetName.replace(/"/g, "");
        getItems(id, entityName, itemSetName).then(function () {
          dfd.resolve();
        },
          dfd.reject);

        return dfd.promise();
      }

      var setVisibility = function () {
        if (!page.ui) {
          self.Visible(false);
          return;
        }

        switch (page.ui.getFormType()) {
          case formType.FORM_TYPE_CREATE:
          case formType.FORM_TYPE_UPDATE:
          case formType.FORM_TYPE_READ_ONLY:
          case formType.FORM_TYPE_DISABLED:
            self.Visible(true);
            break;
          default:
            self.Visible(false);
        }
      };

      var setReadOnlyMode = function () {
        var readOnlyMode;
        if (self.AllowUpdate()) {
          switch (page.ui.getFormType()) {
            case formType.FORM_TYPE_CREATE:
              readOnlyMode = !self.SavingAttr.getUserPrivilege().canCreate;
              break;
            case formType.FORM_TYPE_UPDATE:
              readOnlyMode = !self.SavingAttr.getUserPrivilege().canUpdate;
              break;
            case formType.FORM_TYPE_READ_ONLY:
            case formType.FORM_TYPE_DISABLED:
              readOnlyMode = true;
              break;
            default:
              readOnlyMode = true;
          }
        } else {
          readOnlyMode = true;
        }

        self.ReadOnlyMode(readOnlyMode);
      };

      var formTypeChangeHandler = function() {
        try {
          var initialType = page.ui.getFormType();
          setTimeout(function() {
            var currentType = page.ui.getFormType();
            if (currentType !== initialType) {
              setReadOnlyMode();
            }
          }, 1000);
        } catch (e) {} 
      };

      var attachOnFormLoad = function() {
        try {
          page.data.entity.addOnSave(formTypeChangeHandler);
        } catch (e) {} 
      };

      //var subscribeOnItemSetSelectedChange = function () {
      //  self.ItemSetSelected.subscribe(function (changes) {
      //    changes.forEach(function (change) {
      //      if (change.status === 'added' || change.status === 'deleted') {
      //        var value = buildValueForSavingAttr();
      //        self.SavingAttr.setSubmitMode("always");
      //        self.SavingAttr.setValue(value);
      //      }
      //    });

      //  }, null, "arrayChange");
      //}

      self.Initialize = function () {
        attachOnFormLoad();
        var dfd = $.Deferred();
        setVisibility();
        if (!self.Visible()) {
          self.ModeMessage("The content of this section is not available in such form mode.");
          self.DataLoaded(true);
          dfd.resolve();
          return dfd.promise();
        }

        getData().then(
          function () {
            var selectizeMode = self.SelectizeMode();
            var savingAttrValue = getCurrentSavingAttrValueSet();
            var pluginErrorLoading = !!savingAttrValue;
            setReadOnlyMode();
            var checkedValues = pluginErrorLoading ? $(savingAttrValue) : null;
            var result = [];
            for (var i = 0, max = self.Items.length; i < max; i++) {
              var item = self.Items[i];
              var selected;
              if (savingAttrValue) {
                selected = $.inArray(item.Id, checkedValues) > -1;
              } else {
                selected = item.Selected;
              }

              if (selectizeMode) {
                if (selected) {
                  self.ItemSetSelected.push(item.Id);
                }
              } else {
                var selectedObservable = ko.observable(selected);
                selectedObservable.subscribe(onSelectedChanged);
                item.Value = selectedObservable;
              }

              result.push(item);
            }

            self.ItemSet = ko.observableArray(result);
            var submitModeName = pluginErrorLoading ? "always" : "never";
            self.SavingAttr.setSubmitMode(submitModeName);
            var value = buildValueForSavingAttr();

            self.SavingAttr.setValue(value);
            if (!selectizeMode) {
              var resultByRows = [],
                row,
                colCount = self.ColumnCount();
              for (var j = 0, m = result.length; j < m; j++) {
                if (j % colCount === 0) {
                  if (row) {
                    resultByRows.push(row);
                  }

                  row = [];
                }

                // ReSharper disable once QualifiedExpressionMaybeNull
                // ReSharper disable once UsageOfPossiblyUnassignedValue
                row.push(result[j]);
              }

              if (row) {
                resultByRows.push(row);
              }

              self.ItemSetByRows = ko.observableArray(resultByRows);
            }

            self.DataLoaded(true);
            dfd.resolve();
          },
          function (error) {
            var message;
            if (error) {
              if (error.message) {
                message = "Error: " + error.message;
              } else {
                message = error.toString();
              }

            } else {
              message = "There is no error details.";
            }

            self.DataLoaded(true);
            self.ErrorMessage(message);
            dfd.reject(error);
          }
        );

        return dfd.promise();
      };

      self.SetItemSetSelected = function(selectedItems) {
        self.ItemSetSelected(selectedItems);
        var value = buildValueForSavingAttr();
        self.SavingAttr.setSubmitMode("always");
        self.SavingAttr.setValue(value);
      };
    };

    return {
      ViewModel: viewModel
    };
  }();
})(window.xrmjQuery, window.xrmjQuery, window.CrmWebApiFacade);