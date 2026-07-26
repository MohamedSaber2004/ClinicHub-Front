(function () {
    'use strict';

    function handleResponse(response) {
        return response.json().then(function (body) {
            if (!body.success) {
                throw new Error(body.message || 'Request failed');
            }
            return body.data;
        });
    }

    function apiGet(path) {
        return fetch(path, { headers: { 'Accept-Language': 'ar' } }).then(handleResponse);
    }

    function apiPost(path, body) {
        return fetch(path, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'Accept-Language': 'ar' },
            body: JSON.stringify(body)
        }).then(handleResponse);
    }

    var svgPaths = {
        calendar: 'M19 3h-1V1h-2v2H8V1H6v2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V8h14v11z',
        check: 'M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z',
        clock: 'M13 3h-2v10l8.29 5.29 1-1.72L13 12.36V3zM12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8z',
        completed: 'M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z'
    };

    var statIcons = {
        totalAppointments: { label: 'مواعيد اليوم', color: 'primary', path: svgPaths.calendar },
        checkedIn: { label: 'تم تسجيل الوصول', color: 'green', path: svgPaths.check },
        waiting: { label: 'قيد الانتظار', color: 'amber', path: svgPaths.clock },
        completed: { label: 'تم الانتهاء', color: 'blue', path: svgPaths.completed }
    };

    var statusConfig = {
        pending: { label: 'قيد الانتظار', class: 'badge-warning' },
        confirmed: { label: 'مؤكد', class: 'badge-success' },
        cancelled: { label: 'ملغي', class: 'badge-danger' },
        completed: { label: 'منتهي', class: 'badge-info' },
        'in-progress': { label: 'قيد الكشف', class: 'badge-primary' },
        waiting: { label: 'في الانتظار', class: 'badge-warning' },
        registered: { label: 'تم التسجيل', class: 'badge-info' },
        accepted: { label: 'مقبول', class: 'badge-success' },
        rejected: { label: 'مرفوض', class: 'badge-danger' }
    };

    function getStatusConfig(status, statusLabel, statusClass) {
        if (statusLabel && typeof statusLabel === 'string' && statusLabel.trim()) return { label: statusLabel, class: statusClass || 'badge-info' };
        if (status == null || status === '') return { label: '', class: 'badge-info' };
        var key = String(status).toLowerCase();
        return statusConfig[key] || { label: String(status), class: 'badge-info' };
    }

    function escapeHtml(str) {
        if (!str) return '';
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(str));
        return div.innerHTML;
    }

    function renderStats(stats) {
        var grid = document.getElementById('statsGrid');
        if (!grid) return;
        var html = '';
        var keys = ['totalAppointments', 'checkedIn', 'waiting', 'completed'];
        keys.forEach(function (key) {
            var icon = statIcons[key];
            if (!icon) return;
            html += '<div class="stat-card">'
                + '<div class="stat-info">'
                + '<span class="stat-value">' + escapeHtml(String(stats[key] ?? 0)) + '</span>'
                + '<span class="stat-label">' + icon.label + '</span>'
                + '</div>'
                + '<div class="icon-wrapper icon-wrapper--' + icon.color + '">'
                + '<svg viewBox="0 0 24 24" fill="currentColor"><path d="' + icon.path + '"/></svg>'
                + '</div>'
                + '</div>';
        });
        grid.innerHTML = html;
    }

    function renderQueueTable(items, tbodyId, showQueueNumber, showActions) {
        var tbody = document.getElementById(tbodyId);
        if (!tbody) return;
        if (!items || items.length === 0) {
            var colspan = showActions ? 6 : 5;
            tbody.innerHTML = '<tr><td colspan="' + colspan + '" style="text-align:center;padding:32px;color:var(--text-muted);">لا يوجد مرضى في الطابور</td></tr>';
            return;
        }
        var html = '';
        items.forEach(function (item) {
            var patient = item.patient || {};
            var doctor = item.doctor || {};
            var sc = getStatusConfig(item.status, item.statusLabel, item.statusClass);
            html += '<tr>'
                + (showQueueNumber ? '<td><span style="font-weight:700;font-size:16px;">' + item.queueNumber + '</span></td>' : '')
                + '<td>'
                + '<div style="display:flex;align-items:center;gap:8px;">'
                + '<div class="sidebar-profile-avatar" style="width:28px;height:28px;">'
                + '<span style="font-size:11px;">' + escapeHtml(patient.initial || '') + '</span>'
                + '</div>'
                + escapeHtml(patient.name || '')
                + '</div>'
                + '</td>'
                + '<td>' + escapeHtml(doctor.name || '') + '</td>'
                + '<td>' + escapeHtml(item.time || '') + '</td>'
                + '<td class="status-cell"><span class="badge ' + sc.class + '">' + (sc.label || '—') + '</span></td>'
                + (showActions
                    ? '<td class="queue-actions" data-id="' + escapeHtml(item.id || item.appointmentId || '') + '">' + getQueueActions(item) + '</td>'
                    : '')
                + '</tr>';
        });
        tbody.innerHTML = html;
    }

    function getQueueActions(item) {
        if (item.status === 'waiting' || item.status === 'registered') {
            return '<button class="btn btn-sm btn-outline-primary btn-checkin" data-id="' + escapeHtml(item.id || '') + '"><i class="bi bi-check2"></i> تسجيل الوصول</button>';
        }
        if (item.status === 'in-progress') {
            return '<button class="btn btn-sm btn-outline-success btn-complete" data-id="' + escapeHtml(item.id || '') + '"><i class="bi bi-check-lg"></i> إنهاء</button>';
        }
        if (item.status === 'completed') {
            return '<span class="badge badge-success">مكتمل</span>';
        }
        var fallback = getStatusConfig(item.status, item.statusLabel, item.statusClass);
        return '<span class="badge ' + fallback.class + '">' + fallback.label + '</span>';
    }

    function renderAppointments(items, tbodyId) {
        var tbody = document.getElementById(tbodyId);
        if (!tbody) return;
        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" style="text-align:center;padding:32px;color:var(--text-muted);">لا توجد مواعيد</td></tr>';
            return;
        }
        var html = '';
        items.forEach(function (app) {
            var patient = app.patient || {};
            var doctor = app.doctor || {};
            var sc = getStatusConfig(app.status, app.statusLabel, app.statusClass);
            var actions = getAppointmentActions(app);
            html += '<tr>'
                + '<td>'
                + '<div style="display:flex;align-items:center;gap:8px;">'
                + '<div class="sidebar-profile-avatar" style="width:28px;height:28px;">'
                + '<span style="font-size:11px;">' + escapeHtml(patient.initial || '') + '</span>'
                + '</div>'
                + escapeHtml(patient.name || '')
                + '</div>'
                + '</td>'
                + '<td>' + escapeHtml(doctor.name || '') + '</td>'
                + '<td>' + escapeHtml(app.specialty || '') + '</td>'
                + '<td>' + escapeHtml(app.date || '') + '</td>'
                + '<td>' + escapeHtml(app.time || '') + '</td>'
                + '<td class="status-cell"><span class="badge ' + sc.class + '">' + (sc.label || '—') + '</span></td>'
                + '<td class="appointment-actions" data-id="' + escapeHtml(app.id || '') + '">'
                + actions
                + '</td>'
                + '</tr>';
        });
        tbody.innerHTML = html;
    }

    function getAppointmentActions(app) {
        if (app.status === 'pending') {
            return '<div style="display:flex;gap:4px;">'
                + '<button class="btn btn-sm btn-outline-success btn-approve" data-id="' + escapeHtml(app.id || '') + '"><i class="bi bi-check-lg"></i></button>'
                + '<button class="btn btn-sm btn-outline-danger btn-reject" data-id="' + escapeHtml(app.id || '') + '"><i class="bi bi-x-lg"></i></button>'
                + '</div>';
        }
        if (app.status === 'confirmed' || app.status === 'accepted') {
            return '<button class="btn btn-sm btn-outline-primary btn-checkin" data-id="' + escapeHtml(app.id || '') + '"><i class="bi bi-box-arrow-in-right"></i> تسجيل وصول</button>';
        }
        return '';
    }

    function renderDoctorSchedule(data) {
        var tbody = document.getElementById('scheduleBody');
        if (!tbody) return;
        var appointments = data.appointments || [];
        if (appointments.length === 0) {
            tbody.innerHTML = '<tr><td colspan="4" style="text-align:center;padding:32px;color:var(--text-muted);">لا توجد مواعيد لهذا اليوم</td></tr>';
            return;
        }
        var html = '';
        appointments.forEach(function (app) {
            var patient = app.patient || {};
            var sc = getStatusConfig(app.status, app.statusLabel, app.statusClass);
            html += '<tr>'
                + '<td>'
                + '<div style="display:flex;align-items:center;gap:8px;">'
                + '<div class="sidebar-profile-avatar" style="width:28px;height:28px;">'
                + '<span style="font-size:11px;">' + escapeHtml(patient.initial || '') + '</span>'
                + '</div>'
                + escapeHtml(patient.name || '')
                + '</div>'
                + '</td>'
                + '<td>' + escapeHtml(data.date || '') + '</td>'
                + '<td>' + escapeHtml(app.time || '') + '</td>'
                + '<td class="status-cell"><span class="badge ' + sc.class + '">' + (sc.label || '—') + '</span></td>'
                + '</tr>';
        });
        tbody.innerHTML = html;
    }

    function renderDoctorDropdown(doctors, selectId) {
        var select = document.getElementById(selectId);
        if (!select) return;
        var html = '<option value="">-- اختر الطبيب --</option>';
        doctors.forEach(function (doc) {
            html += '<option value="' + escapeHtml(doc.id) + '">' + escapeHtml(doc.name) + ' - ' + escapeHtml(doc.specialty) + '</option>';
        });
        select.innerHTML = html;
    }

    function renderPagination(containerId, currentPage, totalPages, onPageChange) {
        var container = document.getElementById(containerId);
        if (!container) return;
        if (!totalPages || totalPages <= 0) { container.innerHTML = ''; return; }
        if (totalPages === 1) {
            container.innerHTML = '<nav class="pagination-nav"><ul class="pagination-list"><li class="pagination-item"><span class="pagination-link pagination-link--active">1</span></li></ul></nav>';
            return;
        }
        var html = '<nav class="pagination-nav"><ul class="pagination-list">';
        html += '<li><a class="pagination-link" data-page="' + (currentPage - 1) + '" style="' + (currentPage <= 1 ? 'pointer-events:none;opacity:0.5;' : '') + '">&laquo;</a></li>';
        var start = Math.max(1, currentPage - 2);
        var end = Math.min(totalPages, currentPage + 2);
        for (var i = start; i <= end; i++) {
            html += '<li><a class="pagination-link' + (i === currentPage ? ' pagination-link--active' : '') + '" data-page="' + i + '">' + i + '</a></li>';
        }
        html += '<li><a class="pagination-link" data-page="' + (currentPage + 1) + '" style="' + (currentPage >= totalPages ? 'pointer-events:none;opacity:0.5;' : '') + '">&raquo;</a></li>';
        html += '</ul></nav>';
        container.innerHTML = html;
        $(container).off('click').on('click', '.pagination-link', function () {
            var page = parseInt($(this).data('page'));
            if (!isNaN(page) && page >= 1 && page <= totalPages && page !== currentPage) {
                onPageChange(page);
            }
        });
    }

    var base = window.staffApiBase || '/Staff';

    window.StaffDashboardService = {

        getStats: function () {
            return apiGet(base + '/GetStats');
        },

        getAppointments: function (params) {
            var query = new URLSearchParams(params || {}).toString();
            return apiGet(base + '/GetAppointments' + (query ? '?' + query : ''));
        },

        getQueue: function () {
            return apiGet(base + '/GetQueue');
        },

        approveAppointment: function (id) {
            return apiPost(base + '/ApproveAppointment/' + id);
        },

        rejectAppointment: function (id, reason) {
            return apiPost(base + '/RejectAppointment/' + id, { reason: reason || '' });
        },

        checkInPatient: function (id) {
            return apiPost(base + '/CheckIn/' + id);
        },

        completeAppointment: function (id) {
            return apiPost(base + '/Complete/' + id);
        },

        registerPatient: function (data) {
            return apiPost(base + '/RegisterPatient', data);
        },

        getDoctors: function () {
            return apiGet(base + '/GetDoctors');
        },

        getDoctorSchedule: function (doctorId, date) {
            var query = date ? '?date=' + date : '';
            return apiGet(base + '/GetDoctorSchedule/' + doctorId + (query ? '?' + query : ''));
        },

        renderStats: renderStats,
        renderQueueTable: renderQueueTable,
        renderAppointments: renderAppointments,
        renderDoctorSchedule: renderDoctorSchedule,
        renderDoctorDropdown: renderDoctorDropdown,
        renderPagination: renderPagination,
        getStatusConfig: getStatusConfig
    };
})();
