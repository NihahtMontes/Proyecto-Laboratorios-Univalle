/* Formulario de grupo (Pages/Catalogs/StudentGroups): materias del semestre con docente,
   nombre del grupo = letra + semestre, sugerencia de la siguiente letra libre. */
(function ($) {
    'use strict';

    var configEl = document.getElementById('sgConfig');
    if (!configEl) return;
    var cfg = JSON.parse(configEl.textContent || '{}');
    var isEdit = !!cfg.isEdit;

    var $form = $('#sgForm');
    var $career = $('#sgCareer');
    var $semester = $('#sgSemester');
    var $letter = $('#sgLetter');
    var $rows = $('#sgSubjectRows');

    // Estado por materia: { selected, teacher, lab }. En Crear, toda materia nueva aparece marcada.
    var state = {};
    (cfg.input.subjects || []).forEach(function (s) {
        state[s.subjectId] = { selected: !!s.selected, teacher: s.teacherPersonId || '', lab: s.laboratoryId || '' };
    });
    var letterTouched = isEdit || ($letter.val() || '').trim().length > 0;

    function careerId() { return Number(isEdit ? cfg.input.careerId : $career.val()) || 0; }
    function semester() { return Number(isEdit ? cfg.input.semester : $semester.val()) || 0; }
    function semesterLabel(n) { return n + (n === 1 || n === 3 ? '.er' : '.º') + ' semestre'; }

    function composeName(letter, sem) {
        var clean = (letter || '').trim().toUpperCase();
        return /^[A-Z]{1,3}$/.test(clean) && sem ? clean + sem : clean;
    }

    function existingHere() {
        return (cfg.existingGroups || []).filter(function (g) {
            return g.careerId === careerId() && g.semester === semester();
        });
    }

    function fillSemesters(initial) {
        if (isEdit) return;
        var c = careerId();
        var seen = {};
        var semesters = (cfg.subjects || [])
            .filter(function (s) { return s.careerId === c; })
            .map(function (s) { return s.semester; })
            .filter(function (n) { if (seen[n]) return false; seen[n] = true; return true; })
            .sort(function (a, b) { return a - b; });
        var current = initial || Number($semester.val()) || 0;
        $semester.empty().append($('<option/>', { value: '' }).text('-- Semestre --'));
        semesters.forEach(function (n) {
            $semester.append($('<option/>', { value: n, selected: n === current }).text(semesterLabel(n)));
        });
    }

    function suggestLetter() {
        if (letterTouched || !careerId() || !semester()) return;
        var used = {};
        existingHere().forEach(function (g) { used[(g.letter || '').toUpperCase()] = true; });
        for (var i = 0; i < 26; i++) {
            var candidate = String.fromCharCode(65 + i);
            if (!used[candidate]) { $letter.val(candidate); return; }
        }
    }

    function updateName() {
        var name = composeName($letter.val(), semester());
        $('#sgNamePreview').text(name ? 'Grupo ' + name : '—');
        var existing = existingHere();
        var duplicate = existing.some(function (g) { return g.name === name; });
        $('#sgNamePreview').toggleClass('text-danger', duplicate);
        var $existing = $('#sgExisting').empty();
        if (existing.length) {
            $existing.append(document.createTextNode('Ya existen en este semestre: '));
            $('<strong/>').text(existing.map(function (g) { return g.name; }).join(', ')).appendTo($existing);
        }
        if (duplicate) {
            $existing.append($('<span class="text-danger d-block font-weight-bold"/>').text('El grupo ' + name + ' ya existe: use otra letra.'));
        }
    }

    function optionList(items, emptyText, selected) {
        var $select = $('<select class="form-control form-control-sm"/>');
        $select.append($('<option/>', { value: '' }).text(emptyText));
        items.forEach(function (item) {
            $select.append($('<option/>', { value: item.id, selected: String(item.id) === String(selected) }).text(item.name));
        });
        return $select;
    }

    function renderSubjects() {
        $rows.find('select').each(function () {
            if ($(this).data('select2')) $(this).select2('destroy');
        });
        $rows.empty();
        var subjects = (cfg.subjects || []).filter(function (s) {
            return s.careerId === careerId() && s.semester === semester();
        });
        $('#sgSubjectsEmpty').toggle(subjects.length === 0);
        $('#sgSubjectsTable').toggle(subjects.length > 0);
        if (subjects.length === 0 && careerId() && semester()) {
            $('#sgSubjectsEmpty').text('Este semestre no tiene materias activas registradas.');
        }

        subjects.forEach(function (subject, index) {
            var st = state[subject.id] || (state[subject.id] = { selected: !isEdit, teacher: '', lab: '' });
            var prefix = 'Input.Subjects[' + index + '].';
            var $tr = $('<tr/>').toggleClass('sg-row--off', !st.selected);

            var $check = $('<input type="checkbox" class="sg-check" value="true"/>')
                .attr('name', prefix + 'Selected').prop('checked', st.selected);
            $('<td class="align-middle"/>')
                .append($('<input type="hidden"/>').attr('name', prefix + 'SubjectId').val(subject.id))
                .append($check)
                .appendTo($tr);

            var $name = $('<td class="align-middle"/>');
            $('<span class="font-weight-bold text-dark d-block"/>').text(subject.name).appendTo($name);
            if (subject.code) $('<small class="text-muted"/>').text(subject.code).appendTo($name);
            $tr.append($name);

            var $teacher = optionList(cfg.teachers || [], '-- Docente --', st.teacher)
                .attr('name', prefix + 'TeacherPersonId').addClass('sg-teacher');
            $('<td/>').append($teacher).appendTo($tr);

            var $lab = optionList(cfg.laboratories || [], 'Habitual del grupo', st.lab)
                .attr('name', prefix + 'LaboratoryId');
            $('<td/>').append($lab).appendTo($tr);

            $check.on('change', function () {
                st.selected = this.checked;
                $tr.toggleClass('sg-row--off', !st.selected);
                updateCount();
            });
            $teacher.on('change', function () { st.teacher = $(this).val(); $tr.removeClass('sg-row--error'); });
            $lab.on('change', function () { st.lab = $(this).val(); });

            $rows.append($tr);
            if ($.fn.select2) {
                $teacher.select2({ width: '100%', placeholder: '-- Docente --' });
                $lab.select2({ width: '100%' });
            }
        });
        updateCount();
    }

    function updateCount() {
        var total = $rows.find('.sg-check').length;
        var checked = $rows.find('.sg-check:checked').length;
        $('#sgSubjectCount').text(total ? checked + ' de ' + total + ' materias' : '—');
    }

    function onScopeChange() {
        suggestLetter();
        updateName();
        renderSubjects();
    }

    // ---------- Eventos ----------
    $('#sgManagement').on('change', function () {
        var url = new URL(window.location.href);
        url.searchParams.set('managementId', $(this).val());
        window.location.href = url.toString();
    });
    $career.on('change', function () { fillSemesters(); onScopeChange(); });
    $semester.on('change', onScopeChange);
    $letter.on('input', function () {
        letterTouched = true;
        this.value = this.value.toUpperCase();
        updateName();
    });
    $('#sgCheckAll, #sgCheckNone').on('click', function () {
        var on = this.id === 'sgCheckAll';
        $rows.find('.sg-check').prop('checked', on).trigger('change');
    });
    if ($.fn.select2) $('.sg-select2').select2({ width: '100%' });

    $form.on('submit', function (e) {
        var form = this;
        if (form.dataset.confirmed === 'true') return true;
        e.preventDefault();

        var missing = [];
        $rows.find('tr').each(function () {
            var $tr = $(this);
            if ($tr.find('.sg-check').is(':checked') && !$tr.find('.sg-teacher').val()) {
                $tr.addClass('sg-row--error');
                missing.push($tr.find('td:eq(1) span').text());
            }
        });
        var warn = function (text) {
            if (typeof Swal !== 'undefined') Swal.fire({ icon: 'warning', title: 'Revise el grupo', text: text, confirmButtonColor: '#2962ff' });
            else alert(text);
        };
        if (!careerId() || !semester()) { warn('Elija la carrera y el semestre del grupo.'); return; }
        if (!($letter.val() || '').trim()) { warn('Escriba la letra del grupo (A, B, C...).'); return; }
        if ($rows.find('.sg-check:checked').length === 0) { warn('Marque al menos una materia del semestre.'); return; }
        if (missing.length) { warn('Asigne el docente de: ' + missing.join(', ') + '.'); return; }

        var submit = function () {
            form.dataset.confirmed = 'true';
            HTMLFormElement.prototype.submit.call(form);
        };
        if (!isEdit || typeof Swal === 'undefined') { submit(); return; }
        Swal.fire({
            title: '¿Guardar cambios del grupo?',
            text: 'Las materias desmarcadas se quitarán del grupo junto con su horario.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, guardar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#f59e0b',
            cancelButtonColor: '#6c757d'
        }).then(function (result) {
            if (result && (result.isConfirmed === true || result.value === true)) submit();
        });
    });

    // ---------- Inicio ----------
    fillSemesters(cfg.input.semester);
    onScopeChange();
})(jQuery);
