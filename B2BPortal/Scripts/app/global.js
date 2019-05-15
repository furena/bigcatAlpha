var lTimezone, lTimezoneAbb, bSkipSBInit;

//global initializer
$(function () {
    lTimezone = moment.tz.guess();
    lTimezoneAbb = moment().tz(lTimezone).zoneName();
    $.fx.speeds._default = 200;
});

$(document).ajaxComplete(function () {
    hideAjax();
});
$(document).ajaxStart(function () {
    $(".ui-loader")
        .addClass("glyphicon-refresh-animate")
        .css("visibility", "visible");
});
function hideAjax() {
    $(".ui-loader")
        .removeClass("glyphicon-refresh-animate")
        .css("visibility", "hidden");
}
$(function () {
    //initialize help icons
    hideAjax();
    $("label.addHelp")
        .each(function (i, o) {
            var help = $(this).closest("div").children("div.notes").eq(0).text();
            $(this)
                .tooltip({ title: help, placement: "auto", trigger: "manual" });
        })
        .on("mouseenter click", function (e) {
            if (e.offsetX > (e.target.offsetWidth - 15)) {
                $(this).tooltip("show");
                return false;
            }
        })
        .on("mouseleave", function () {
            $(this).tooltip("hide");
        });
});

var localErr = {};
$(document).ajaxError(function (event, xhr, ajaxOptions, thrownError) {
    if (typeof xhr.responseJSON == "object") {
        localErr = xhr.responseJSON;
        var msg = (localErr.ErrorMessage || localErr.ExceptionMessage);
        if (msg != null && msg.length > 0) {
            alert(msg);
            return;
        }

        localErr.thrownError = thrownError;
    } else {
        localErr.Message = xhr.responseText;
        localErr.thrownError = (thrownError || "Unknown");
    }
    alert(localErr.Message);
});
var SiteUtil = function () {
    function isValidEmailAddress(emailAddress) {
        var pattern = new RegExp(/^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$/i);
        return pattern.test(emailAddress);
    }
    var ErrorImages = {
        Warning: '/content/images/warning.png',
        Default: '/content/images/info.png'
    };

    function _showHelp(title, body) {
        _showModal({ title: "Help - " + title, body: body });
    }

    function _showModal(options) {
        options.okHide = (options.okHide == null) ? true : options.okHide;
        options.title = (options.title == null) ? "Message" : options.title;
        options.id = (options.id == null) ? "nAlertDialog" : options.id;
        var dialogId = "#" + options.id;
        var btn = {
            OK: function () {
                $(dialogId).modal("hide");
                return false;
            }
        };
        if (typeof(options.callback)!="undefined" && options.callback !== null) {
            if (options.callback.length > 0) {
                options.body += "<br><textarea rows='3' cols='50' style='margin-top:5px' id='nDialogVal' size='20'></textarea>";
            }
            btn.Cancel = function () {
                $(dialogId).modal("hide");
                return false;
            };
            btn.OK = function () {
                var res = $("#nDialogVal").val();
                options.callback(res);
                if (options.okHide) $(dialogId).modal("hide");
            };
        }
        var modal = _getModal({
            title: options.title,
            buttons: btn,
            id: options.id,
            body: options.body,
            modalClass: options.modalClass,
            displayCallback: options.displayCallback
        });
        $("#nDialogVal").focus();
        modal.setContent = function (body) {
            modal.body.html(body);
        };
        return modal;
    }
    function _getModal(opts) {
        var res = $("<div/>").addClass("modal fade").attr("id", opts.id || "nAlertDialog");
        var d = $("<div/>").addClass("modal-dialog").appendTo(res);
        if (opts.modalClass) {
            d.addClass(opts.modalClass);
        }
        var c = $("<div/>").addClass("modal-content").appendTo(d);
        var h = $("<div/>").addClass("modal-header bg-primary").appendTo(c);
        $("<button/>").attr({ "type": "button", "data-dismiss": "modal", "aria-hidden": "true" }).addClass("close").html("&times;").appendTo(h);
        $("<h4/>").addClass("modal-title").html(opts.title || "Alert").appendTo(h);

        var body = $("<div/>").addClass("modal-body").appendTo(c);
        if (typeof opts.body == "object") {
            body.append(opts.body);
        } else {
            body.html(opts.body);
        }
        //body.append("<img src='/images/ajax-loader-dialog.gif' class='ajaxloader ajaxloader-dialog' />");

        if (opts.buttons) {
            var f = $("<div/>").addClass("modal-footer").appendTo(c);
            for (var button in opts.buttons) {
                $("<button/>").attr({ "type": "button" }).addClass("btn btn-default").on("click", opts.buttons[button]).html(button).appendTo(f);
            }
        }
        res.on("hidden.bs.modal", function () {
            $(this).remove();
        });
        res.body = body;
        return res.modal().on("shown.bs.modal", function () {
            if (opts.displayCallback) opts.displayCallback();
        });
    }
    function _deTc(sTitle) {
        var re = /([a-z])([A-Z])/g;
        var s = sTitle.replace(re, "$1 $2");
        return s;
    }
    function _ajaxCall(url, data, callback, method, successMessage) {
        method = (method == null) ? "GET" : method;
        successMessage = (successMessage == null) ? "" : successMessage;
        if (method !== "GET") {
            data = JSON.stringify(data);
        }
        $.ajax({
            url: url,
            data: data,
            type: method,
            withCredentials: true,
            contentType: "application/json",
            success: function (res, status, xhr) {
                if (successMessage.length > 0) {
                    notifySuccess("Success", successMessage);
                }
                if (callback) callback(res, xhr);
            }
        });
    }

    function getFormObjects(obj, excludeClasses) {
        obj = $(obj);
        excludeClasses = (excludeClasses == null) ? "" : "," + excludeClasses;
        var oForm = obj
            .find("input,textarea,select")
            .not(":submit, :button, :reset, :image, [disabled]" + excludeClasses);

        var radios = $.grep(oForm, function (el) { return el.type == "radio"; });
        var newForm = $.grep(oForm, function (el) { return el.type == "radio"; }, true);
        var newradios = radios.unique();
        $(newradios).each(function (i, o) {
            var r = $('input[name=' + this + ']:checked');
            if (r.length == 0) r = $('input[name=' + this + ']');   //they're all false, so get all and will return the 1st one by default
            r = r.clone()[0];
            if (r.length == 0) $(r).val("");   //they're all false, so Set to "" to no changes made
            r.id = this;
            newForm.push(r);
        });
        return $(newForm);
    }

    function getDataObject(obj, fnMod) {
        var oForm = getFormObjects(obj);
        var oOut = {};
        oForm.each(function () {
            var o = $(this).clone(true, true)[0];
            o.value = this.value;   //sanity check on the clone method, which appears to not do what I want on SELECT objects
            if ((o.tagName == "INPUT") && (o.type == "checkbox")) {
                oOut[o.name] = o.checked;
                return;
            }
            var s = $(o).val();
            var val = ((s == null) ? "" : s.replace(/"(?:[^"\\]|\\.)*"/g, "\{0}"));
            if (oOut[o.name]) {
                if (oOut[o.name].constructor !== Array) {
                    var t = oOut[o.name];
                    oOut[o.name] = [];
                    oOut[o.name].push(t);
                }
                oOut[o.name].push(val);
            } else {
                oOut[o.name] = val;
            }
        });
        return oOut;
    }

    var alertImages = {
        info: "info.png",
        error: "error.png",
        warning: "warning.png",
        success: "success.png"
    }
    function getOrdinal(num) {
        var num2 = num.slice(-1);
        switch (num2) {
            case "1":
                return (num.slice(-2) == "11" ? "th" : "st");
            case "2":
                return (num.slice(-2) == "12" ? "th" : "nd");
            case "3":
                return (num.slice(-2) == "13" ? "th" : "rd");
            default:
                return "th";
        }
    }
    function _pluralize(label, count) {
        return count.toString() + " " + ((count == 1) ? label : label + "s");
    }

    function addCommas(nStr) {
        nStr += '';
        x = nStr.split('.');
        x1 = x[0];
        x2 = x.length > 1 ? '.' + x[1] : '';
        var rgx = /(\d+)(\d{3})/;
        while (rgx.test(x1)) {
            x1 = x1.replace(rgx, '$1' + ',' + '$2');
        }
        return x1 + x2;
    }
    function _utcToLocal(sDate, sInputFormatMask, sOutputFormatMask, bIncludeTZAbb) {
        bIncludeTZAbb = (bIncludeTZAbb == null) ? true : bIncludeTZAbb;
        sOutputFormatMask = sOutputFormatMask || 'MM/DD/YYYY h:mm A';
        var bIsUTC = (typeof sDate == "string" && sDate.indexOf("T") == 10);
        sInputFormatMask = (bIsUTC) ? null : (sInputFormatMask || 'MM/DD/YYYY HH:mm A');
        var res = moment.utc(sDate, sInputFormatMask).tz(lTimezone).format(sOutputFormatMask);
        if (bIncludeTZAbb) res += " " + lTimezoneAbb;
        return (res.indexOf("Invalid date") > -1) ? "N/A" : res;
    }
    function copyToClipboard(text) {
        if (window.clipboardData && window.clipboardData.setData) {
            // IE specific code path to prevent textarea being shown while dialog is visible.
            return clipboardData.setData("Text", text);

        } else if (document.queryCommandSupported && document.queryCommandSupported("copy")) {
            var textarea = document.createElement("textarea");
            textarea.textContent = text;
            textarea.style.position = "fixed";  // Prevent scrolling to bottom of page in MS Edge.
            document.body.appendChild(textarea);
            textarea.select();
            try {
                return document.execCommand("copy");  // Security exception may be thrown by some browsers.
            } catch (ex) {
                console.warn("Copy to clipboard failed.", ex);
                return false;
            } finally {
                document.body.removeChild(textarea);
            }
        }
    } return {
        Copy: copyToClipboard,
        AddCommas: addCommas,
        GetOrdinal: getOrdinal,
        DeTC: _deTc,
        ShowModal: _showModal,
        GetModal: _getModal,
        Pluralize: _pluralize,
        UtcToLocal: _utcToLocal,
        ShowHelp: _showHelp,
        AlertImages: alertImages,
        AjaxCall: _ajaxCall,
        ErrorImages: ErrorImages,
        GetFormObjects: getFormObjects,
        GetDataObject: getDataObject,
        IsValidEmailAddress: isValidEmailAddress,
    };
}();
Array.prototype.unique = function () {
    var o = {}, i, l = this.length, r = [];
    for (i = 0; i < l; i += 1) o[this[i].name] = this[i].name;
    for (i in o) r.push(o[i]);
    return r;
};
$.fn.serializeObject = function () {
    var o = {};
    var a = this.serializeArray();
    $.each(a, function () {
        if (o[this.name] !== undefined) {
            if (!o[this.name].push) {
                o[this.name] = [o[this.name]];
            }
            o[this.name].push(this.value || '');
        } else {
            o[this.name] = this.value || '';
        }
    });
    return o;
};