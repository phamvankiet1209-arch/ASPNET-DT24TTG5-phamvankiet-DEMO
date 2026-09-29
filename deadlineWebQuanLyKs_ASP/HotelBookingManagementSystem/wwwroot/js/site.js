$(function () {
    const toast = (icon, title) => {
        if (window.Swal) Swal.fire({ toast: true, position: "top-end", icon, title, showConfirmButton: false, timer: 2600 });
    };

    if (window.successMessage) toast("success", window.successMessage);
    if (window.errorMessage) toast("error", window.errorMessage);

    const nav = document.querySelector(".navbar-lux");
    const onScroll = () => {
        if (!nav) return;
        nav.classList.toggle("scrolled", window.scrollY > 12);
    };
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });

    const io = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (!entry.isIntersecting) return;
            entry.target.classList.add("in-view");
            io.unobserve(entry.target);
        });
    }, { threshold: 0.12 });
    document.querySelectorAll("[data-animate]").forEach((el, i) => {
        el.style.transitionDelay = `${Math.min(i % 6, 5) * 0.08}s`;
        io.observe(el);
    });

    $(document).on("click", "[data-confirm]", function (e) {
        e.preventDefault();
        const form = $(this).closest("form");
        const href = $(this).attr("href");
        const msg = $(this).data("confirm") || "Bạn chắc chắn muốn thực hiện?";
        Swal.fire({
            title: "Xác nhận",
            text: msg,
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#1B7A7A",
            cancelButtonText: "Hủy",
            confirmButtonText: "Đồng ý"
        }).then((r) => {
            if (!r.isConfirmed) return;
            if (form.length) form.submit();
            else if (href) window.location = href;
        });
    });

    $(document).on("click", ".js-fav", function () {
        const btn = $(this);
        const id = btn.data("id");
        $.post("/Favorite/Toggle", { roomId: id }, function (res) {
            toast("success", res.message);
            btn.toggleClass("text-danger", res.isFavorite);
            btn.find("i").toggleClass("fa-solid", res.isFavorite).toggleClass("fa-regular", !res.isFavorite);
        });
    });

    $("#voucherBtn").on("click", function () {
        const payload = {
            roomId: $("#RoomId").val(),
            checkIn: $("#CheckInDate").val(),
            checkOut: $("#CheckOutDate").val(),
            rooms: $("#NumberOfRooms").val() || 1,
            voucherCode: $("#VoucherCode").val(),
            serviceIds: $("input[name='ServiceIds']:checked").map(function () { return this.value; }).get().join(",")
        };
        $.post("/Booking/ApplyVoucher", payload, function (res) {
            toast(res.success ? "success" : "error", res.message);
            if (res.totalText) $("#totalPay").text(res.totalText);
            if (res.discountText) $("#discountPay").text(res.discountText);
        });
    });

    $("#checkAvailBtn").on("click", function () {
        $.get("/Room/CheckAvailability", {
            roomId: $(this).data("id"),
            checkIn: $("#ci").val(),
            checkOut: $("#co").val()
        }, function (res) {
            $("#availResult").removeClass("d-none alert-success alert-danger")
                .addClass(res.available ? "alert-success" : "alert-danger")
                .text(res.message + (res.totalText ? " Tổng ước tính: " + res.totalText : ""));
        });
    });

    $(".gallery-thumb").on("click", function () {
        $("#mainGallery").attr("src", $(this).attr("src"));
        $(".gallery-thumb").removeClass("active");
        $(this).addClass("active");
    });
});
