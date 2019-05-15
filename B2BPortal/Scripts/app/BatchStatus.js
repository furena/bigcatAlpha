$(function () {
    $("#btnRetry").on("click", function () {
        if (!confirm("Are you sure you want to retry processing this queue?")) {
            return;
        }
        var data = $("#BatchDetails").data("data");
        var d = { id: data.Submission.id };
        var url = "/api/Admin/RequeueRequest";
        SiteUtil.AjaxCall(url, d, function (res) {
            refreshStatus(d.id);
            SiteUtil.ShowModal({ title: "Retry Submitted", body: "The batch has been re-queued for processing. It may take a few minutes for the batch processor to begin - click the item in the list to refresh." });
        }, "POST");
    });
    $("#btnKill").on("click", function () {
        if (!confirm("This will remove the batch from the queue and prevent further processing. Are you sure you want to continue?"))
            return;
        var data = $("#BatchDetails").data("data");
        var d = { id: data.Submission.id };
        var url = "/api/Admin/KillQueuedRequest";
        SiteUtil.AjaxCall(url, d, function (res) {
            location.reload();
        }, "POST");
    });
    $("#btnDelete").on("click", function () {
        if (!confirm("This will delete the batch, all associated guest requests, and history. Are you sure you want to continue?"))
            return;
        var data = $("#BatchDetails").data("data");
        var d = { id: data.Submission.id };
        var url = "/api/Admin/DeleteQueuedRequest";
        SiteUtil.AjaxCall(url, d, function (res) {
            location.reload();
        }, "POST");
    });
    $("#btnSubmissionHistory").on("click", function () {
        showSubmissionHistory();
    });
    $("#btnHelp").on("click", function () {
        SiteUtil.ShowHelp("Batch Processing Status", $("#HelpContent").html());
    });
    $("#invitees").on("click", function () {
        var data = $("#BatchDetails").data("data");
        getItemDetail(data.Submission.id, this.value);
    });
    $("#btnInvite").on("click", function () {
        location.href = "/Admin/BulkInvite";
    });

    $("input[name=HistoryType]").on("click", function () {
        checkType();
        loadBatchItems();
    });

    function checkType() {
        var t = $("input[name=HistoryType]:checked").val();
        var o = $("#HistoryDays")
        switch (t) {
            case "GetBatchHistory":
                o.removeAttr("disabled");
                break;
            case "GetBatchPending":
                o.attr("disabled", "disabled");
                break;
        }
    }
    $("#btnReload").on("click", function () {
        loadBatchItems();
    });

    function getItemDetail(batchId, email) {
        var data = { submissionId: batchId, email: email };

        var url = "/api/Admin/GetBulkItemStatus";
        SiteUtil.AjaxCall(url, data, function (res) {
            if (res == null)
                return;

            showDetail(res);
        });
    }
    function showDetail(data) {
        var request = data.Request;
        var response = (data.Response!=null) ? data.Response.body : null;
        var requestH = $("<div/>");
        var i = 0;
        for (col in request) {
            i++;
            var ds = "";
            var bg = (i % 2 == 0) ? "#fafafa;" : "";
            if (request[col] != null) {
                switch (col) {
                    case "docType":
                        continue;
                    case "invitationResult":
                        ds = GetInvitationResult(request[col]);
                        break;
                    case "lastModDate":
                    case "requestDate":
                        ds = (new Date(request[col])).toLocaleString();
                        break;
                    case "disposition":
                        ds = getDis(request[col]);
                        break;
                    default:
                        ds = request[col].toString().replace(/\r\n/g, "<br/>");
                }
                if (ds.length > 0) {
                    var d = $("<div/>").css({ "backgroundColor": bg }).appendTo(requestH);
                    d.html("<span class='label'>" + SiteUtil.DeTC(col) + "</span><span class='data'>" + ds + "</span>");
                }
            }
        }
        var responseH = $("<div/>");
        var i = 0;
        var status = "";
        var responseObj = [];
        var responseTxt = "Not processed yet."
        if (response != null) {
            for (col in response) {
                i++;
                var ds = "";
                var bg = (i % 2 == 0) ? "#fafafa;" : "";
                if (response[col] != null) {
                    switch (col) {
                        case "@odata.context":
                        case "invitedUserMessageInfo":
                        case "invitedUser":
                            break;
                        case "status":
                            status = response[col].toString();
                            ds = response[col].toString().replace(/\r\n/g, "<br/>");
                            break;
                        case "errorInfo":
                            if (status == "Error") {
                                ds = response[col].toString().replace(/\r\n/g, "<br/>");
                            }
                            break;
                        default:
                            ds = response[col].toString().replace(/\r\n/g, "<br/>");
                    }
                    if (ds.length > 0) {
                        var d = $("<div/>").css("backgroundColor", bg).appendTo(responseH);
                        d.html("<span class='label'>" + SiteUtil.DeTC(col) + "</span><span class='data'>" + ds + "</span>");
                    }
                }
            }
            responseObj = responseH.children();
            responseTxt = "";
        }
        $("#inviteDetails h4.modal-title").html("Detail - " + request.emailAddress);
        $("#request").html("").append(requestH.children());
        $("#response").html(responseTxt).append(responseObj);
        $("#inviteDetails").modal();
        $('#inviteDetails a:first').tab('show');
     }
    function GetInvitationResult(data) {
        var res = $("<div/>");
        var i = 0;
        for (col in data) {
            i++;
            var bg = (i % 2 == 0) ? "#fafafa;" : "";
            var ds = "";
            if (data[col] != null) {
                var d = $("<div/>").css("backgroundColor", bg).appendTo(res);
                switch (col) {
                    case "lastModDate":
                    case "requestDate":
                        ds = (new Date(data[col])).toLocaleString();
                        break;
                    case "disposition":
                        ds = getDis(data[col]);
                        break;
                    default:
                        ds = data[col].toString().replace(/\r\n/g, "<br/>");
                }
                d.html("<span class='label'>" + SiteUtil.DeTC(col) + "</span><span class='data'>" + ds + "</span>");
            }
        }
        return res.html();
    }

    function getDis(dis) {
        switch (dis) {
            case 0:
                return "Approved";
            case 1:
                return "AutoApproved";
            case 2:
                return "Denied";
            case 3:
                return "Pending";
            case 4:
                return "QueuePending";
            default:
                return "Unknown (" + dis + ")";
        }
    }
    function getMemType(t) {
        switch (t) {
            case 0:
                return "Guest";
            case 1:
                return "Member";
            default:
                return "Unknown (" + t + ")";
        }
    }
    function loadBatchItems() {
        var t = $("input[name=HistoryType]:checked").val();
        var d = $("#HistoryDays").val();
        var url = "/api/Admin/" + t + "/" + d;
        SiteUtil.AjaxCall(url, null, function (res) {
            loadList(res);
            //check to see if a batch was specified
            if (location.hash.indexOf("#id") > -1) {
                var id = location.hash.split('=')[1];
                SiteUtil.AjaxCall("/api/Admin/GetBatchItem/" + id, null, function (res) {
                    loadItem(res);
                });
            }
        });
    }
    function loadBatchPending() {
        SiteUtil.AjaxCall("/api/Admin/GetBatchPending", null, function (res) {
            loadList(res);
        });
    }

    function loadList(items) {
        $("#BatchList button").remove();
        if (items.length == 0) {
            $("<button>").html("No batches found matching that filter.").addClass("list-group-item").appendTo("#BatchList");
            return;
        }
        $(items).each(function (i, o) {
            var s = "<span class='badge'>" + o.itemsProcessed + "/" + o.itemsSubmitted + "</span>" + new Date(o.submissionDate).toLocaleString();
            $("<button>")
                .html(s)
                .data("data", o)
                .addClass("list-group-item")
                .on("click", function () {
                    refreshStatus(o.id);
                })
                .appendTo("#BatchList");
        });
    }
    function showSubmissionHistory() {
        $("#resultDetails div.modal-body").html("No history yet.");

        var data = $("#BatchDetails").data("history");
        $("#resultDetails h4.modal-title").html("Process History");
        var d = $("<div/>").css("overflow", "hidden");
        if (data.length == 0) {
            $("#resultDetails").modal();
            return;
        }

        $(data).each(function (i, o) {
            var r = $("<div/>");
            $("<span/>")
                .addClass("hist")
                .html((new Date(o.processingDate)).toLocaleString())
                .appendTo(r);
            $("<span/>")
                .on("click", function () {
                    var detail = $("<div/>").addClass("showCode").html(o.errorMessage);
                    SiteUtil.GetModal({title:"Error Details", body: detail, modalClass:"errorDetail"});
                })
                .html(o.errorMessage!=null ? "[click for error]" : "No Errors")
                .addClass("hist histB")
                .appendTo(r);
            r.css("clear", "both").appendTo(d);
        });

        $("#resultDetails div.modal-body").html("").append(d);
        $("#resultDetails").modal();
    }
    function loadItem(data) {
        var url = "/api/Admin/GetBatchProcessingHistory/" + data.Submission.id;
        SiteUtil.AjaxCall(url, null, function (res) {
            $("#BatchDetails").data("history", res);
            $("#batchAttempts").html(SiteUtil.Pluralize("attempt", res.length));
        }, "GET");

        var batch = data.Submission;
        var results = data.BatchResults;

        $("#BatchDetails").data("data", data);
        $("button.batchAction").css("display", (batch.itemsProcessed == batch.itemsSubmitted) ? "none" : "block");
        $("#btnRetry").css("display", (batch.itemsProcessed < batch.itemsSubmitted) ? "block" : "none");
        $("#invitees option").remove();
        $("#groupAssignments option").remove();

        $("#submissionDate").html(new Date(batch.submissionDate).toLocaleString());
        $("#batchId").html(batch.id);
        $("#invitationMessage").html(batch.invitationMessage);
        $("#itemsProcessed").html(batch.itemsProcessed);
        $("#itemsSubmitted").html(batch.itemsSubmitted);
        $("#memberType").html(getMemType(batch.memberType));
        $(batch.groupList).each(function (i, o) {
            $("<option>").html(o).appendTo("#groupAssignments");
        });
        var arr = batch.emailString.split("\n");
        $(arr).each(function (i, o) {
            $("<option>").html(o).appendTo("#invitees");
        });

    }
    function refreshStatus(id) {
        SiteUtil.AjaxCall("/api/Admin/GetBatchItem/" + id, null, function (res) {
            loadItem(res);
        });
    }

    loadBatchItems();
    checkType();
});