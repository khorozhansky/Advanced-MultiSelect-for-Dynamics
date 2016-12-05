/// <reference path="libraries/jquery.js" />
/// <reference path="XmlUtils.js" />
var TuneXrm = window.TuneXrm || { __namespace: true };
TuneXrm.AdvFindUtils = function () {
  /* UNSUPPORTED */

  var getAdvFindContentWindow = function (iframeName) {
    return xrmjQuery("#" + iframeName, parent.document).get(0).contentWindow;
  };

  var getAdvFindControl = function (iframeName) {
    var contWin = getAdvFindContentWindow(iframeName);
    if (!contWin || !contWin.$find) {
      return null;
    }

    return contWin.$find("advFind");
  };

  var getEntityTypeCode = function(entityLogicalName) {
    return parent.Mscrm.EntityPropUtil.EntityTypeName2CodeMap[entityLogicalName];
  };

  var setFetchXmlAndLayout = function (advFind, fetchXmlText) {
    advFind.ResetControl();
    advFind.set_fetchXml(fetchXmlText);
    var layoutXml = xrmjQuery.parseXML(advFind.get_layoutXml());
    var $layoutXml = xrmjQuery(layoutXml);
    $layoutXml.find("cell").each(function () { xrmjQuery(this).remove(); });
    var rowInLayout = $layoutXml.find("row");
    var fetchXml = xrmjQuery.parseXML(fetchXmlText);
    var $fetchXml = xrmjQuery(fetchXml);
    var entityNode = $fetchXml.find('entity');
    var firstLevelAttributes = entityNode.find('attribute');
    firstLevelAttributes.each(function () {
      var fieldName = xrmjQuery(this).attr("name");
      var cellNode = xrmjQuery("<cell>", $layoutXml);
      cellNode.attr("name", fieldName);
      cellNode.attr("width", "200");
      rowInLayout.append(cellNode);
    });

    var layoutXmlString = TuneXrm.XmlUtils.XmlToString(layoutXml);
    advFind.set_layoutXml(layoutXmlString);
  };

  var setSrc = function (iframeName, entityLogicalName, fetchXmlText, onLoadErrorHandler) {
    var baseUrl = Xrm.Page.context.getClientUrl();
    var entityTypeCode = getEntityTypeCode(entityLogicalName);
    console.log("entity: " + entityLogicalName);
    var url = baseUrl + "/SFA/goal/ParticipatingQueryCondition.aspx?entitytypecode=" + entityTypeCode;
    var iframeControl = Xrm.Page.ui.controls.get(iframeName);
    var iframeElem = iframeControl.getObject();
    iframeElem.addEventListener("load", function() {
      console.log("query builder loaded");
      try {
        if (fetchXmlText) {
          try {
            var advFind = getAdvFindControl(iframeName);
            if (!advFind) {
              return;
            }

            setFetchXmlAndLayout(advFind, fetchXmlText);
          } catch (e) {
            try {
              onLoadErrorHandler();
              return;
            } catch (e) {

            } 
          }
        }
      } catch (ex) {
        console.error(ex);
      } 
    });

    iframeControl.setSrc(url);
  };

  var clearSrc = function (iframeName) {
    var iframeControl = Xrm.Page.ui.controls.get(iframeName);
    iframeControl.setSrc("about:blank");
  };

  var addOnAdvFindChange = function (iframeName, onChangeHandler) {
    var advFind = getAdvFindControl(iframeName);
    advFind.add_onChange(onChangeHandler);
  };

  return {
    GetAdvFindControl: getAdvFindControl,
    SetSrc: setSrc,
    ClearSrc: clearSrc,
    AddOnAdvFindChange: addOnAdvFindChange
  }
}();