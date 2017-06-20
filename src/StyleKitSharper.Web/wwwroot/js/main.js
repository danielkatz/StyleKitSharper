(function () {
    var javaEditor = null;
    var csharpEditor = null;

    $(document).ready(function () {

        var java = ace.edit("java");
        java.setTheme("ace/theme/monokai");
        java.setShowPrintMargin(false);
        java.getSession().setMode("ace/mode/java");
        java.getSession().on('change', function (e) {
            onChange();
        });
        javaEditor = java;

        var csharp = ace.edit("csharp");
        csharp.setTheme("ace/theme/monokai");
        csharp.setShowPrintMargin(false);
        csharp.getSession().setMode("ace/mode/csharp");
        csharp.setReadOnly(true);
        csharpEditor = csharp;
    });

    var onChange = debounce(function () {
        if (javaEditor && csharpEditor) {
            var java = javaEditor.getValue();

            if (java.length > 0) {
                var base64 = base64EncodingUTF8(java);

                $.ajax({
                    type: "POST",
                    data: JSON.stringify(base64),
                    url: "api/transform",
                    contentType: "application/json",
                    success: function (data) {
                        csharpEditor.setValue(data);
                    }
                });
            }
        }
    }, 250);

    function base64EncodingUTF8(str) {
        var encoded = new TextEncoderLite("utf-8").encode(str);
        var b64Encoded = base64js.fromByteArray(encoded);
        return b64Encoded;
    }

    // Returns a function, that, as long as it continues to be invoked, will not
    // be triggered. The function will be called after it stops being called for
    // N milliseconds. If `immediate` is passed, trigger the function on the
    // leading edge, instead of the trailing.
    function debounce(func, wait, immediate) {
        var timeout;
        return function () {
            var context = this, args = arguments;
            var later = function () {
                timeout = null;
                if (!immediate) func.apply(context, args);
            };
            var callNow = immediate && !timeout;
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
            if (callNow) func.apply(context, args);
        };
    };
})();
