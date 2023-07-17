/* global Chart */

'use strict';

(function() {
    Chart.plugins.register({
        id: 'datatable-filler',
        beforeInit: function(chart, options) {
            this.element = document.getElementById(options.target);
            this.callback = options.callback;
            this.datasetHeaderRowStyle = options.datasetHeaderRowStyle;
            this.datasetHederCellStyle = options.datasetHeaderCellStyle;
            this.datasetRowStyle = options.datasetRowStyle;
            this.datasetLabelCellStyle = options.datasetLabelCellStyle;
            this.datasetCellStyle = options.datasetCellStyle;
            this.datasetGrandTotalStyle = options.datasetGrandTotalStyle;
        },
        afterUpdate: function (chart) {
            if (!this.element) {
                return;
            }

            var datasets = chart.data.datasets;
            var rowstyle = '';
            var aggregateOperation = chart.config.options.plugins['datatable-filler'].aggregateRows;
            var aggregateLabel = aggregateOperation ? aggregateOperation.replace('sum', 'Total').replace('max', 'Maximum').replace('min', 'Minimum').replace('avg', 'Average') : '';

            if (this.datasetHeaderRowStyle) {
                rowstyle = this.datasetHeaderRowStyle();
            }
            var html = '<table>\n\t<colgroup>\n';

            for (var i = 0; i < chart.data.labels.length + 2; i++) {
                html += '\t\t<col />\n';
            }
            html += '\t<colgroup>\n<tr style="' + rowstyle + '">\n\t<th></th>\n';

            for (var i = 0; i < chart.data.labels.length; i++) {
                var cellstyle = '';

                if (this.datasetHeaderCellStyle) {
                    cellstyle = this.datasetHeaderCellStyle(i);
                }
                html += '\t<th style="' + cellstyle + '">' + chart.data.labels[i] + '</th>\n';
            }
            if (aggregateOperation) {
                html += '\t<th style="' + cellstyle + '">' + aggregateLabel + '</th>\n';
            }
            html += '</tr>\n';

            var totals = [];

            for (var i in datasets) {
                var rowTotal = aggregateOperation == 'min' ? Number.MAX_VALUE : 0;

                if (datasets[i]) {
                    var rowstyle = '';

                    if (this.datasetHeaderRowStyle) {
                        rowstyle = this.datasetRowStyle(i + 1, datasets[i].borderColor, datasets[i].backgroundColor);
                    }

                    var headerstyle = '';

                    if (this.datasetLabelCellStyle) {
                        headerstyle = this.datasetLabelCellStyle(i + 1, datasets[i].borderColor, datasets[i].backgroundColor);
                    }
                    html += '<tr style="' + rowstyle + '">\n\t<th style="' + headerstyle + '">' + datasets[i].label + '</th>\n';

                    for (var j in datasets[i].data) {
                        if (totals.length <= j) {
                            if (aggregateOperation == 'min') {
                                totals.push(Number.MAX_VALUE);
                            }
                            else {
                                totals.push(0);
                            }
                        }
                        var val = datasets[i].data[j];

                        switch (aggregateOperation) {
                            case 'sum':
                                totals[j] += val;
                                rowTotal += val;
                                break;
                            case 'max':
                                totals[j] = Math.max(val, totals[j]);
                                rowTotal = Math.max(val, rowTotal);
                                break;
                            case 'min':
                                totals[j] = Math.min(val, totals[j]);
                                rowTotal = Math.min(val, rowTotal);
                                break;
                            case 'avg':
                                totals[j] += val / datasets.length;
                                rowTotal += val / datasets[i].data.length;
                                break;
                        }

                        if (this.callback) {
                            val = this.callback(val);
                        }
                        var cellstyle = '';

                        if (this.datasetCellStyle) {
                            cellstyle = this.datasetCellStyle(i + 1, j + 1, datasets[i].borderColor, datasets[i].backgroundColor);
                        }
                        html += '\t<td style="' + cellstyle + '">' + val + '</td>\n';
                    }

                    if (aggregateOperation) {
                        if (aggregateOperation == 'avg') {
                            rowstyle = parseInt(rowTotal);
                        }
                        if (this.callback) {
                            rowTotal = this.callback(rowTotal);
                        }
                        html += '\t<th style="' + headerstyle + '">' + rowTotal + '</th>\n';
                    }
                    html += '</tr>\n';
                }
            }

            if (aggregateOperation) {
                var rowstyle = '';

                if (this.datasetHeaderRowStyle) {
                    rowstyle = this.datasetHeaderRowStyle();
                }
                html += '<tr style="' + rowstyle + '">\n\t<th>' + aggregateLabel + '</th>\n';

                for (var i = 0; i < totals.length; i++) {
                    var cellstyle = '';
                    var total = 0;

                    if (this.datasetHeaderCellStyle) {
                        cellstyle = this.datasetHeaderCellStyle(i);
                    }

                    if (aggregateOperation == 'avg') {
                        totals[i] = parseInt(totals[i]);
                    }
                    if (this.callback) {
                        total = this.callback(totals[i]);
                    }
                    else {
                        total = totals[i];
                    }
                    html += '\t<th style="' + cellstyle + '">' + total + '</th>\n';
                }

                if (this.datasetGrandTotalStyle) {
                    cellstyle = this.datasetGrandTotalStyle();
                }
                var grandTotal = aggregateOperation == 'min' ? Number.MAX_VALUE : 0;

                for (var i in totals) {
                    switch (aggregateOperation) {
                        case 'sum':
                            grandTotal += totals[i];
                            break;
                        case 'max':
                            grandTotal = Math.max(totals[i], grandTotal);
                            break;
                        case 'min':
                            grandTotal = Math.min(totals[i], grandTotal);
                            break;
                        case 'avg':
                            grandTotal += totals[i] / datasets.length;
                            break;
                    }
                }

                if (aggregateOperation == 'avg') {
                    grandTotal = parseInt(grandTotal);
                }

                if (this.callback) {
                    grandTotal = this.callback(grandTotal);
                }
                html += '\<th style="' + cellstyle + '">' + grandTotal + '</th>\n</tr>\n';
            }
            html += '</table>\n';

            this.element.innerHTML = html;
        }
    });
}());