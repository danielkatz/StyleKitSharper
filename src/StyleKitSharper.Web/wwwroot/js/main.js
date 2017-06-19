(function () {

    $(document).ready(function () {

        $("#btnConvert").on('click', onConvert);

    });

    var onConvert = function () {
        var java = $(".java-input").val();

        if (java.length > 0) {
            var base64 = base64EncodingUTF8(java);

            $.ajax({
                type: "POST",
                data: JSON.stringify(base64),
                url: "api/transform",
                contentType: "application/json",
                success: function (data) {
                    $(".csharp-output").text(data);
                }
            });
        }
    };

    var base64EncodingUTF8 = function (str) {
        var encoded = new TextEncoderLite("utf-8").encode(str);
        var b64Encoded = base64js.fromByteArray(encoded);
        return b64Encoded;
    }
})();
