import 'dart:typed_data';

import 'package:flutter/services.dart' show rootBundle;
import 'package:intl/intl.dart';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;

import 'models/contrato_consigna_model.dart';

/// El contrato de consigna es el único de los 4 documentos de Mis Archivos
/// sin generación de PDF en el backend (ver nota en
/// contrato_consigna_model.dart). Se arma acá, del lado del cliente, a
/// partir de los datos que sí devuelve la API — no es una réplica
/// pixel-perfecta del formato en papel, pero cubre el mismo contenido
/// (encabezado, partes, vehículo, cláusulas, firmas) con los datos reales
/// del contrato en vez de texto fijo.
Future<Uint8List> construirContratoPdf(ContratoConsignaModel contrato) async {
  final doc = pw.Document();
  final formatoFecha = DateFormat('dd/MM/yyyy');
  final formatoMonto = NumberFormat.decimalPattern('es');
  final logoBytes = await rootBundle.load('assets/images/logo.png');
  final logo = pw.MemoryImage(logoBytes.buffer.asUint8List());

  doc.addPage(
    pw.Page(
      pageFormat: PdfPageFormat.a4,
      margin: const pw.EdgeInsets.all(32),
      build: (context) => pw.Column(
        crossAxisAlignment: pw.CrossAxisAlignment.start,
        children: [
          pw.Row(
            mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
            crossAxisAlignment: pw.CrossAxisAlignment.start,
            children: [
              pw.Row(
                children: [
                  pw.SizedBox(width: 44, height: 44, child: pw.Image(logo)),
                  pw.SizedBox(width: 10),
                  pw.Column(
                    crossAxisAlignment: pw.CrossAxisAlignment.start,
                    children: [
                      pw.Text(
                        'A&J AUTOS USADOS',
                        style: pw.TextStyle(
                          fontSize: 11,
                          fontWeight: pw.FontWeight.bold,
                        ),
                      ),
                      pw.Text(
                        'PLAYAAUTOS S.A.',
                        style: const pw.TextStyle(fontSize: 9),
                      ),
                      pw.Text(
                        'ASUNCIÓN, PARAGUAY',
                        style: const pw.TextStyle(fontSize: 8),
                      ),
                    ],
                  ),
                ],
              ),
              pw.Container(
                padding: const pw.EdgeInsets.all(6),
                decoration: pw.BoxDecoration(
                  border: pw.Border.all(width: 0.5),
                ),
                child: pw.Text(
                  'FECHA: ${formatoFecha.format(contrato.fechaGeneracion)}',
                  style: const pw.TextStyle(fontSize: 9),
                ),
              ),
            ],
          ),
          pw.SizedBox(height: 20),
          pw.Center(
            child: pw.Text(
              'CONTRATO DE CONSIGNACIÓN AUTOMOTRIZ\nPARA COMPRAVENTA',
              textAlign: pw.TextAlign.center,
              style: pw.TextStyle(
                fontSize: 13,
                fontWeight: pw.FontWeight.bold,
              ),
            ),
          ),
          pw.SizedBox(height: 4),
          pw.Center(
            child: pw.Text(
              'N° ${contrato.numeroContrato}',
              style: const pw.TextStyle(fontSize: 10),
            ),
          ),
          pw.SizedBox(height: 16),
          pw.Text(
            'En la ciudad de Asunción, en la empresa A&J Autos Usados - '
            'PlayaAutos S.A., denominada la CONSIGNATARIA, y el/la señor/a '
            '${contrato.consignanteNombre}'
            '${contrato.consignanteCI != null ? ', identificado/a con cédula/RUC número ${contrato.consignanteCI}' : ''}, '
            'denominado/a el/la CONSIGNANTE, se conviene a presentar el '
            'contrato de consignación, sujeto a las cláusulas y condiciones '
            'siguientes.',
            style: const pw.TextStyle(fontSize: 9.5),
          ),
          pw.SizedBox(height: 14),
          pw.Text(
            'PRIMERA. El CONSIGNANTE deja en consignación para vender, el '
            'automóvil de su propiedad con la siguiente información:',
            style: const pw.TextStyle(fontSize: 9.5),
          ),
          pw.SizedBox(height: 8),
          ..._tablasVehiculo(contrato),
          pw.SizedBox(height: 14),
          _clausula(
            'SEGUNDA',
            'El CONSIGNANTE recibirá Gs. ${formatoMonto.format(contrato.precioVentaCliente)} '
                'por la venta de su vehículo al concretarse la operación. La '
                'CONSIGNATARIA podrá venderlo al precio que estime '
                'conveniente, siendo cualquier diferencia a su favor. Además, '
                'percibirá una comisión del ${contrato.porcentajeComision.toStringAsFixed(1)}% sobre el '
                'precio base por sus servicios de intermediación.',
          ),
          _clausula(
            'TERCERA',
            'El plazo del presente contrato se conviene en ${contrato.duracionDias} '
                'días, a partir de la fecha ${formatoFecha.format(contrato.fechaInicio)}, cuyo '
                'vencimiento opera el día ${formatoFecha.format(contrato.fechaFin)}, a cuyo '
                'vencimiento, la CONSIGNATARIA retirará la unidad o procederá '
                'según acuerdo entre las partes.',
          ),
          _clausula(
            'CUARTA',
            'El vehículo permanecerá durante la vigencia del presente '
                'contrato depositado en el local de A&J Autos Usados - '
                'PlayaAutos S.A., quedando prohibido su uso por parte del '
                'CONSIGNANTE o cualquier otra persona, salvo en el caso de '
                'demostración de funcionamiento, debiendo ser conducido por '
                'la CONSIGNATARIA o persona autorizada.',
          ),
          _clausula(
            'QUINTA',
            'La CONSIGNATARIA se responsabiliza de todos los daños que '
                'pudiera ocasionar a la unidad o a terceros durante toda la '
                'vigencia del presente contrato, y hasta la devolución del '
                'vehículo a satisfacción del propietario.',
          ),
          _clausula(
            'SEXTA',
            'El CONSIGNANTE manifiesta que el vehículo no registra gravamen '
                'de ninguna especie, ni deudas impositivas, o en concepto de '
                'multas, responsabilizándose por evicción y vicios '
                'redhibitorios.',
          ),
          if (contrato.clausulas != null &&
              contrato.clausulas!.trim().isNotEmpty) ...[
            pw.SizedBox(height: 6),
            pw.Text(contrato.clausulas!, style: const pw.TextStyle(fontSize: 9.5)),
          ],
          pw.SizedBox(height: 10),
          pw.Text(
            'Se firman los ejemplares de ley y cada parte recibe el suyo en '
            'este acto.',
            style: const pw.TextStyle(fontSize: 9.5),
          ),
          pw.SizedBox(height: 36),
          pw.Row(
            mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
            children: [
              _firma('EL CONSIGNANTE', contrato.consignanteNombre,
                  contrato.consignanteCI != null
                      ? 'C.I. / RUC: ${contrato.consignanteCI}'
                      : null),
              _firma('LA CONSIGNATARIA', 'A&J Autos Usados - PlayaAutos S.A.',
                  'Representante Legal'),
            ],
          ),
        ],
      ),
    ),
  );

  return doc.save();
}

List<pw.Widget> _tablasVehiculo(ContratoConsignaModel contrato) {
  pw.Widget celda(String texto, {bool header = false}) => pw.Padding(
        padding: const pw.EdgeInsets.all(5),
        child: pw.Text(
          texto,
          style: pw.TextStyle(
            fontSize: 9,
            fontWeight: header ? pw.FontWeight.bold : null,
          ),
        ),
      );

  return [
    pw.Table(
      border: pw.TableBorder.all(width: 0.5),
      children: [
        pw.TableRow(
          children: [
            celda('MARCA', header: true),
            celda('MODELO / LÍNEA', header: true),
            celda('AÑO', header: true),
            celda('TIPO', header: true),
          ],
        ),
        pw.TableRow(
          children: [
            celda(contrato.vehiculoMarca),
            celda(contrato.vehiculoModelo),
            celda('${contrato.vehiculoAnio}'),
            celda(contrato.vehiculoTipo ?? '-'),
          ],
        ),
      ],
    ),
    pw.Table(
      border: pw.TableBorder.all(width: 0.5),
      children: [
        pw.TableRow(
          children: [
            celda('COLOR', header: true),
            celda('KILOMETRAJE', header: true),
          ],
        ),
        pw.TableRow(
          children: [
            celda(contrato.vehiculoColor ?? '-'),
            celda(
              contrato.vehiculoKilometraje != null
                  ? '${contrato.vehiculoKilometraje} km'
                  : '-',
            ),
          ],
        ),
      ],
    ),
  ];
}

pw.Widget _clausula(String numero, String texto) {
  return pw.Padding(
    padding: const pw.EdgeInsets.only(bottom: 8),
    child: pw.RichText(
      text: pw.TextSpan(
        style: const pw.TextStyle(fontSize: 9.5),
        children: [
          pw.TextSpan(
            text: '$numero. ',
            style: pw.TextStyle(fontWeight: pw.FontWeight.bold),
          ),
          pw.TextSpan(text: texto),
        ],
      ),
    ),
  );
}

pw.Widget _firma(String rol, String nombre, String? detalle) {
  return pw.Column(
    children: [
      pw.Container(width: 170, height: 0.5, color: PdfColors.black),
      pw.SizedBox(height: 4),
      pw.Text(
        rol,
        style: pw.TextStyle(fontSize: 9, fontWeight: pw.FontWeight.bold),
      ),
      pw.Text(nombre, style: const pw.TextStyle(fontSize: 9)),
      if (detalle != null)
        pw.Text(detalle, style: const pw.TextStyle(fontSize: 8)),
    ],
  );
}
