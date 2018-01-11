/// <reference path="js-vsdoc/Xrm.Page.js" />
/// <reference path="FormSaveModes.js" />
var AdvancedMultiSelect = window.AdvancedMultiSelect || { __namespace: true };

AdvancedMultiSelect.FormUtils = function () {
  var preventAutoSave = function (context) {
    var eventArgs = context.getEventArgs();
    var saveMode = eventArgs.getSaveMode();
    var prevent = saveMode === AdvancedMultiSelect.FormSaveModes.AUTOSAVE || saveMode === AdvancedMultiSelect.FormSaveModes.SAVE_AND_CLOSE;
    if (prevent) {
      eventArgs.preventDefault();
      return true;
    }

    return false;
  }

  var reload = function (preventSavePrompt) {
    if (preventSavePrompt) {
      Xrm.Page.data.entity.attributes.forEach(function (attr) {
        attr.setSubmitMode("never");
      });

      Xrm.Page.data.setFormDirty(false);
    }

    Xrm.Utility.openEntityForm(Xrm.Page.data.entity.getEntityName(), Xrm.Page.data.entity.getId());
  }

  var showWaitingNotification = function(notificationId, getIsLoadingInProgress, delay, text) {
    text = text || "Loading, please wait";
    delay = delay || 500;
    var pointerCountLimit = 30;
    var counter = 1;
    var action = function() {
      var show = getIsLoadingInProgress();
      if (!show) {
        Xrm.Page.ui.clearFormNotification(notificationId);
        return;
      }

      if (counter > pointerCountLimit) {
        counter = 1;
      }

      var message = text + Array(counter).join(".");
      Xrm.Page.ui.setFormNotification(message, "INFO", notificationId);
      counter++;
      setTimeout(action, delay);
    };

    action();
  };

  var validateAutocomplete = function (ctrl, getIsValidHandler, additionalErrorMessage) {
    var notificationId = "Autocomplete Validation";
    var attr = Xrm.Page.getAttribute(ctrl.getName());
    var mandatory = attr.getRequiredLevel().toLowerCase() === "required";
    var value = attr.getValue();
    var valid;
    if (!value) {
      valid = mandatory ? false : true;
      if (valid) {
        ctrl.clearNotification(notificationId);
      }

      return valid;
    }

    valid = getIsValidHandler(value);
    if (valid) {
      ctrl.clearNotification(notificationId);
      return true;
    }

    additionalErrorMessage = additionalErrorMessage || "";
    ctrl.setNotification("Please provide a valid value for " + ctrl.getLabel() + ". " + additionalErrorMessage, notificationId);
    return false;
  };

  var showAutocomplete = function (ctrl, dataArray, getItemHandler, itemCountLimit) {
    try {
      var userInput = ctrl.getValue();
      userInput = userInput == null ? "" : userInput.trim().toLowerCase();
      var userInputLength = userInput.length;
      itemCountLimit = itemCountLimit || 1000;
      var resultSet = {};
      var results = new Array();
      for (var i = 0; i < dataArray.length; i++) {
        var item = getItemHandler(userInput, i);
        if (item) {
          results.push(item);

          if (results.length === itemCountLimit) {
            if (results.length < dataArray.length) {
              resultSet.commands = { id: "more items", label: "There are more items..." };
            }

            if (userInputLength !== 0) {
              break;
            }
          }
        }
      }

      resultSet.results = results;
      if (results.length === 0) {
        resultSet.commands = { id: "noitems", label: "No matching options found." };
      }

      ctrl.showAutoComplete(resultSet);
    } catch (e) {
      Xrm.Utility.alertDialog("An error occured while validating autocomple: " + message);
    }
  };

  var onAutocompleteKeyPress = function (ctx, dataArray, getItemHandler, itemCountLimit) {
    showAutocomplete(ctx.getEventSource(), dataArray, getItemHandler, itemCountLimit);
  };

  var getElemById = function (id) {
    /* UNSUPPORTED */
    return parent.document.getElementById(id);
  }

  var getInputElemByControl = function (ctrl) {
    /* UNSUPPORTED */
    return getElemById(ctrl.getName() + "_i");
  }

  return {
    PreventAutoSave: preventAutoSave,
    Reload: reload,
    ShowWaitingNotification: showWaitingNotification,
    ValidateAutocomplete: validateAutocomplete,
    OnAutocompleteKeyPress: onAutocompleteKeyPress,
    ShowAutocomplete: showAutocomplete,
    GetInputElemByControl: getInputElemByControl
  }
}();
