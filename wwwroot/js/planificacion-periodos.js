/* ==========================================================================
   Selector visual de periodos para la planificación de laboratorios.
   Pages/LaboratoryOperations/Planificacion.cshtml

   - Fichas de periodo (1..12) con círculo + hora completa.
   - Clic en una ficha = periodo de inicio; el bloque cubierto por la
     cantidad se pinta como un bloque continuo resaltado.
   - Stepper "− N +" para la cantidad de periodos.
   - Ocupación del ambiente en la fecha (GET handler=Occupancy): fichas
     ocupadas (gris) y fichas de separación contiguas (rayado suave).
   - Horario semanal del grupo (GET handler=GroupSchedule) cuando hay
     materia + grupo + fecha: fichas "Clase del grupo" (verde, sugerido) y
     "El grupo tiene clase de X" (rojo rayado, no clicables como inicio si
     el bloque las tocaría; el servidor bloquea al guardar).
   - Sincroniza los inputs hidden Input.StartPeriodId / Input.PeriodCount.
   ES5, IIFE. Requiere jQuery.
   ========================================================================== */
(function () {
    'use strict';

    var MAX_PERIODS = 12;
    var GAP_TITLE = 'Periodo de separación entre prácticas';
    var DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

    /** Día de semana de una fecha yyyy-MM-dd (0 = domingo, 1..6 = lunes..sábado); -1 si es inválida. */
    function weekdayOfDate(iso) {
        var parts = String(iso || '').split('-');
        if (parts.length !== 3) return -1;
        return new Date(+parts[0], +parts[1] - 1, +parts[2]).getDay();
    }

    function PeriodSelector() {
    }

    PeriodSelector.prototype.init = function ($, config) {
        this.$ = $;
        this.config = config || {};
        this.$grid = $(this.config.grid);
        this.$slots = this.$grid.find('.pp-slot');
        this.$start = $(this.config.startInput);
        this.$count = $(this.config.countInput);
        this.$countLabel = $(this.config.countLabel);
        this.$stepper = $(this.config.stepper);
        this.$range = $(this.config.range);
        this.$occBox = $(this.config.occupancyBox);
        this.$groupBox = $(this.config.groupBox || '#period-group');
        this.$date = $(this.config.dateInput);
        this.$lab = $(this.config.laboratorySelect);
        this.$subject = $(this.config.subjectSelect);
        this.$group = $(this.config.groupInput);
        this.periods = this.config.periods || [];
        this.readOnly = this.config.readOnly === true;
        this.excludeId = this.config.excludeId || 0;
        this.managementId = this.config.managementId || 0;
        this.occupancyUrl = this.config.occupancyUrl || window.location.pathname;
        this.groupScheduleUrl = this.config.groupScheduleUrl || window.location.pathname;
        this.blocks = [];
        this.ownByOrder = {};    // orden → true: clase de ESTA materia (sugerida)
        this.busyByOrder = {};   // orden → materia: clase de OTRA materia del grupo (bloquea)
        this.groupTimer = null;
        this.groupSeq = 0;       // descarta respuestas de grupo fuera de orden

        if (this.$grid.length === 0 || this.periods.length === 0) return;

        // Estado inicial desde los inputs hidden (precarga del calendario o edición).
        var initial = this.periodById(this.$start.val());
        this.startId = initial ? String(initial.id) : String(this.periods[0].id);
        this.count = Number(this.$count.val() || 1);
        if (!(this.count >= 1)) this.count = 1;
        if (this.count > MAX_PERIODS) this.count = MAX_PERIODS;

        this.bind();
        this.syncInputs();
        this.render();
        this.loadOccupancy();
        this.loadGroupSchedule();
    };

    PeriodSelector.prototype.periodById = function (id) {
        var list = this.periods;
        for (var i = 0; i < list.length; i++) {
            if (String(list[i].id) === String(id)) return list[i];
        }
        return null;
    };

    PeriodSelector.prototype.periodByOrder = function (order) {
        var list = this.periods;
        for (var i = 0; i < list.length; i++) {
            if (Number(list[i].order) === Number(order)) return list[i];
        }
        return null;
    };

    PeriodSelector.prototype.selectedOrder = function () {
        var p = this.periodById(this.startId);
        return p ? Number(p.order) : 0;
    };

    /** Cantidad máxima que cabe desde el periodo elegido (Order + N − 1 ≤ 12). */
    PeriodSelector.prototype.maxCount = function () {
        var order = this.selectedOrder();
        return order > 0 ? MAX_PERIODS - order + 1 : 1;
    };

    PeriodSelector.prototype.bind = function () {
        var self = this;

        if (!this.readOnly) {
            this.$slots.on('click', function (e) {
                e.preventDefault();
                var $slot = $(this);
                if ($slot.hasClass('pp-slot--disabled')
                    || $slot.hasClass('pp-slot--occupied')
                    || $slot.hasClass('pp-slot--gap')
                    || $slot.hasClass('pp-slot--group-busy')) {
                    return;
                }
                self.startId = $slot.attr('data-period-id');
                self.clampCount();
                self.syncInputs();
                self.render();
            });

            this.$stepper.on('click', '.pp-stepper__btn', function (e) {
                e.preventDefault();
                var step = Number($(this).attr('data-step')) || 0;
                self.count = Math.min(self.maxCount(), Math.max(1, self.count + step));
                self.syncInputs();
                self.render();
            });
        }

        // La ocupación depende de la fecha y del ambiente elegidos.
        this.$date.on('change', function () {
            self.loadOccupancy();
        });
        this.$lab.on('change', function () {
            self.loadOccupancy();
        });

        // El horario del grupo depende de la materia, del grupo y del día de semana de la fecha.
        this.$date.on('change', function () {
            self.loadGroupSchedule();
        });
        this.$subject.on('change', function () {
            self.loadGroupSchedule();
        });
        this.$group.on('input change', function () {
            clearTimeout(self.groupTimer);
            self.groupTimer = setTimeout(function () {
                self.loadGroupSchedule();
            }, 300);
        });
    };

    PeriodSelector.prototype.clampCount = function () {
        var max = this.maxCount();
        if (this.count > max) this.count = Math.max(1, max);
    };

    PeriodSelector.prototype.syncInputs = function () {
        this.$start.val(this.startId);
        this.$count.val(String(this.count));
    };

    PeriodSelector.prototype.render = function () {
        var self = this;
        var startOrder = this.selectedOrder();
        var lastOrder = startOrder > 0 ? startOrder + this.count - 1 : 0;
        var maxStartOrder = MAX_PERIODS - this.count + 1;

        this.$slots.each(function () {
            var $slot = $(this);
            var order = Number($slot.attr('data-order'));
            var isStart = order === startOrder;
            var inBlock = startOrder > 0 && order >= startOrder && order <= lastOrder;
            var fits = order <= maxStartOrder;
            // El grupo no puede iniciar aquí un bloque que toque una clase de otra materia del grupo.
            var groupBlocked = !self.readOnly && fits
                && self.blockTouchesGroupClass(order, Math.min(MAX_PERIODS, order + self.count - 1)) != null;

            $slot.toggleClass('pp-slot--start', isStart);
            $slot.toggleClass('pp-slot--covered', inBlock);
            $slot.toggleClass('pp-slot--block-start', inBlock && order === startOrder);
            $slot.toggleClass('pp-slot--block-end', inBlock && order === lastOrder);
            $slot.toggleClass('pp-slot--group-warn', groupBlocked);
            $slot.toggleClass('pp-slot--disabled', (!self.readOnly && !fits) || groupBlocked);
            if (!self.readOnly) $slot.prop('disabled', !fits || groupBlocked);
        });

        this.$countLabel.text(String(this.count));
        if (!this.readOnly) {
            this.$stepper.find('.pp-stepper__btn').each(function () {
                var step = Number($(this).attr('data-step')) || 0;
                $(this).prop('disabled', step < 0 ? self.count <= 1 : self.count >= self.maxCount());
            });
        }

        this.updateRange(startOrder, lastOrder);
        this.updateOccupancyWarning(startOrder, lastOrder);
        this.updateGroupWarning(startOrder, lastOrder);
    };

    /** "Horario resultante: 07:35 – 11:15 (Periodos 1–4)". */
    PeriodSelector.prototype.updateRange = function (startOrder, lastOrder) {
        if (!this.$range.length) return;
        var start = this.periodByOrder(startOrder);
        var end = this.periodByOrder(lastOrder);
        if (!start || !end) {
            this.$range.hide();
            return;
        }
        var countText = this.count <= 1 ? 'Periodo ' + startOrder : 'Periodos ' + startOrder + '\u2013' + lastOrder;
        this.$range.html('<i class="far fa-clock mr-1"></i>Horario resultante: <strong>' + start.start + ' \u2013 ' + end.end +
            '</strong> <span class="text-muted font-weight-normal">(' + countText + ')</span>');
        this.$range.show();
    };

    /**
     * Ocupación del ambiente en la fecha: [{startOrder, periodCount, label}].
     * Solo lectura; al editar el handler excluye la planificación actual.
     */
    PeriodSelector.prototype.loadOccupancy = function () {
        var self = this;
        if (this.$occBox.length === 0) return;

        var labId = Number(this.$lab.val() || 0);
        var date = (this.$date.val() || '').trim();
        if (!labId || !DATE_PATTERN.test(date)) {
            this.blocks = [];
            this.applyOccupancy();
            return;
        }

        var params = { handler: 'Occupancy', laboratoryId: labId, date: date };
        if (this.excludeId) params.excludeId = this.excludeId;

        $.get(this.occupancyUrl, params).done(function (data) {
            self.blocks = [];
            (data || []).forEach(function (b) {
                self.blocks.push({
                    start: Number(b.startOrder),
                    count: Number(b.periodCount) || 1,
                    label: b.label || 'Ocupado'
                });
            });
            self.applyOccupancy();
        }).fail(function () {
            self.blocks = [];
            self.applyOccupancy();
        });
    };

    PeriodSelector.prototype.applyOccupancy = function () {
        var self = this;
        var occupied = {};   // order -> etiqueta de la práctica
        var gaps = {};       // order -> true
        // Horario del grupo: depende del día de semana de la fecha, no del ambiente elegido.
        var weekday = weekdayOfDate((this.$date.val() || '').trim());

        this.blocks.forEach(function (b) {
            var last = b.start + b.count - 1;
            for (var o = b.start; o <= last; o++) occupied[o] = b.label;
        });
        // Debe quedar un periodo libre entre prácticas del mismo ambiente:
        // los periodos contiguos a un bloque son de separación (si no están ocupados).
        this.blocks.forEach(function (b) {
            var before = b.start - 1;
            var after = b.start + b.count;
            if (before >= 1 && !occupied[before]) gaps[before] = true;
            if (after <= MAX_PERIODS && !occupied[after]) gaps[after] = true;
        });

        this.$slots.each(function () {
            var $slot = $(this);
            var order = Number($slot.attr('data-order'));
            var p = self.periodByOrder(order);
            var baseTitle = p ? 'Periodo ' + order + ' · ' + p.start + ' \u2013 ' + p.end : 'Periodo ' + order;

            var busySubject = weekday > 0 ? (self.busyByOrder[order] || null) : null;
            var ownClass = !busySubject && weekday > 0 && self.ownByOrder[order] === true;

            $slot.removeClass('pp-slot--occupied pp-slot--gap pp-slot--group-own pp-slot--group-busy');

            var title = baseTitle;
            if (ownClass) {
                $slot.addClass('pp-slot--group-own');
                title = 'Clase del grupo · ' + title;
            }
            if (busySubject) {
                $slot.addClass('pp-slot--group-busy');
                title = 'El grupo tiene clase de ' + busySubject + ' · ' + title;
            }
            if (occupied[order]) {
                $slot.addClass('pp-slot--occupied');
                title = occupied[order] + ' · ' + title;
            } else if (!busySubject && gaps[order]) {
                $slot.addClass('pp-slot--gap');
                title = GAP_TITLE + ' · ' + title;
            }
            $slot.attr('title', title);
        });

        var startOrder = this.selectedOrder();
        this.updateOccupancyWarning(startOrder, startOrder > 0 ? startOrder + this.count - 1 : 0);
        this.updateGroupWarning(startOrder, startOrder > 0 ? startOrder + this.count - 1 : 0);
    };

    /** Materia con clase del grupo dentro del rango indicado (null si el rango está libre). */
    PeriodSelector.prototype.blockTouchesGroupClass = function (startOrder, lastOrder) {
        for (var o = startOrder; o <= lastOrder; o++) {
            if (this.busyByOrder[o]) return this.busyByOrder[o];
        }
        return null;
    };

    /**
     * Horario semanal del grupo (GET handler=GroupSchedule): [{weekday, order}] propios y
     * [{weekday, order, subject}] de otras materias. Solo se conservan los del día de la fecha.
     */
    PeriodSelector.prototype.loadGroupSchedule = function () {
        var self = this;
        if (this.$subject.length === 0 || this.$group.length === 0) return;

        var subjectId = Number(this.$subject.val() || 0);
        var groupName = (this.$group.val() || '').trim();
        var date = (this.$date.val() || '').trim();
        if (!subjectId || !groupName || !DATE_PATTERN.test(date)) {
            this.ownByOrder = {};
            this.busyByOrder = {};
            this.applyOccupancy();   // limpia las marcas
            return;
        }

        var seq = ++this.groupSeq;
        $.get(this.groupScheduleUrl, {
            handler: 'GroupSchedule',
            subjectId: subjectId,
            groupName: groupName,
            managementId: this.managementId
        }).done(function (data) {
            if (seq !== self.groupSeq) return;
            self.ownByOrder = {};
            self.busyByOrder = {};
            var weekday = weekdayOfDate(date);
            ((data && data.own) || []).forEach(function (s) {
                if (Number(s.weekday) === weekday) self.ownByOrder[Number(s.order)] = true;
            });
            ((data && data.busy) || []).forEach(function (s) {
                if (Number(s.weekday) === weekday) self.busyByOrder[Number(s.order)] = s.subject || 'otra materia';
            });
            self.applyOccupancy();
        }).fail(function () {
            if (seq !== self.groupSeq) return;
            self.ownByOrder = {};
            self.busyByOrder = {};
            self.applyOccupancy();
        });
    };

    /** Aviso cuando la selección actual toca una clase de otra materia del grupo (el servidor lo bloquea). */
    PeriodSelector.prototype.updateGroupWarning = function (startOrder, lastOrder) {
        if (!this.$groupBox.length) return;
        var weekday = weekdayOfDate((this.$date.val() || '').trim());
        var subject = weekday > 0 && startOrder > 0 ? this.blockTouchesGroupClass(startOrder, lastOrder) : null;
        if (!subject) {
            this.$groupBox.hide().empty();
            return;
        }
        var groupName = (this.$group.val() || '').trim();
        // La materia y el grupo traen texto de usuario: se insertan como texto.
        this.$groupBox.empty()
            .append('<i class="fas fa-ban text-danger mr-1"></i>')
            .append($('<span class="text-danger font-weight-bold"></span>').text(
                'El grupo ' + groupName + ' tiene clase de ' + subject + ' en ese horario: elija otro periodo.'));
        this.$groupBox.show();
    };

    /** Aviso cuando la selección actual choca con la ocupación (solape o sin periodo libre). */
    PeriodSelector.prototype.updateOccupancyWarning = function (startOrder, lastOrder) {
        if (!this.$occBox.length) return;
        if (!startOrder || this.blocks.length === 0) {
            this.$occBox.hide().empty();
            return;
        }
        var clashes = this.blocks.filter(function (b) {
            var last = b.start + b.count - 1;
            return startOrder <= last + 1 && b.start <= lastOrder + 1;
        });
        if (clashes.length === 0) {
            this.$occBox.hide().empty();
            return;
        }
        var labels = [];
        clashes.forEach(function (b) {
            if (labels.indexOf(b.label) === -1) labels.push(b.label);
        });
        // Las etiquetas traen texto de usuario (práctica, materia, grupo): se insertan como texto.
        this.$occBox.empty()
            .append('<i class="fas fa-exclamation-triangle text-warning mr-1"></i>')
            .append($('<span class="text-warning"></span>').text(
                'El ambiente está ocupado o no queda un periodo libre entre prácticas: ' + labels.join('; ') + '.'));
        this.$occBox.show();
    };

    window.PeriodSelector = {
        create: function (config) {
            var $ = window.jQuery;
            if (!$) return null;
            var instance = new PeriodSelector();
            instance.init($, config);
            return instance;
        }
    };
})();
