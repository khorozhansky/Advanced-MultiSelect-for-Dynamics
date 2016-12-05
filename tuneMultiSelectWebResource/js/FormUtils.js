/// <reference path="FormSaveModes.js" />
var TuneXrm = window.TuneXrm || { __namespace: true };

TuneXrm.FormUtils = function () {
  var preventAutoSave = function (context) {
    var eventArgs = context.getEventArgs();
    var saveMode = eventArgs.getSaveMode();
    var prevent = saveMode === TuneXrm.FormSaveModes.AUTOSAVE || saveMode === TuneXrm.FormSaveModes.SAVE_AND_CLOSE;
    if (prevent) {
      eventArgs.preventDefault();
      return true;
    }

    return false;
  }

  var reload = function (preventSavePrompt) {
    if (preventSavePrompt) {
      Xrm.Page.data.entity.attributes.forEach(function (attr, index) {
        attr.setSubmitMode("never");
      });

      Xrm.Page.data.setFormDirty(false);
    }

    Xrm.Utility.openEntityForm(Xrm.Page.data.entity.getEntityName(), Xrm.Page.data.entity.getId());
  }

  var showLoadingNotification = function(getIsLoadingInProgress, delay, text) {
    text = text || "Loading, please wait";
    delay = delay || 500;
    var id = "LoadingProgress";
    var pointerCountLimit = 30;
    var counter = 1;
    var action = function() {
      var show = getIsLoadingInProgress();
      if (!show) {
        Xrm.Page.ui.clearFormNotification(id);
        return;
      }

      if (counter > pointerCountLimit) {
        counter = 1;
      }

      var message = text + Array(counter).join(".");
      Xrm.Page.ui.setFormNotification(message, "INFO", id);
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
    if (mandatory && !value) {
      return false;
    }

    var valid = getIsValidHandler(value);
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
      Xrm.Utility.alertDialog("An error occured while validating autocomple. Details:" + message);
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
    ShowLoadingNotification: showLoadingNotification,
    ValidateAutocomplete: validateAutocomplete,
    OnAutocompleteKeyPress: onAutocompleteKeyPress,
    ShowAutocomplete: showAutocomplete,
    GetInputElemByControl: getInputElemByControl
  }
}();
