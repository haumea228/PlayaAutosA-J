import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:printing/printing.dart';

typedef CargarPdfBytes = Future<Uint8List> Function();

/// Fila de un documento dentro de un acordeón de Mis Archivos: muestra
/// título/subtítulo y dos acciones, "Ver" (preview en pantalla completa) y
/// "Descargar" (comparte el PDF vía el selector del sistema — en
/// Android/iOS no hay forma de "guardar en Descargas" sin permisos extra de
/// almacenamiento, así que compartir es el equivalente estándar).
class DocumentoCard extends StatefulWidget {
  const DocumentoCard({
    super.key,
    required this.icono,
    required this.titulo,
    required this.subtitulo,
    required this.nombreArchivo,
    required this.cargarPdf,
  });

  final IconData icono;
  final String titulo;
  final String subtitulo;
  final String nombreArchivo;
  final CargarPdfBytes cargarPdf;

  @override
  State<DocumentoCard> createState() => _DocumentoCardState();
}

class _DocumentoCardState extends State<DocumentoCard> {
  bool _cargando = false;

  Future<Uint8List?> _obtenerBytes() async {
    setState(() => _cargando = true);
    try {
      return await widget.cargarPdf();
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('No se pudo obtener el documento.')),
        );
      }
      return null;
    } finally {
      if (mounted) setState(() => _cargando = false);
    }
  }

  Future<void> _ver() async {
    final bytes = await _obtenerBytes();
    if (bytes == null || !mounted) return;
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => VisorPdfScreen(bytes: bytes, titulo: widget.titulo),
      ),
    );
  }

  Future<void> _descargar() async {
    final bytes = await _obtenerBytes();
    if (bytes == null) return;
    await Printing.sharePdf(
      bytes: bytes,
      filename: '${widget.nombreArchivo}.pdf',
    );
  }

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;

    return Card(
      margin: const EdgeInsets.symmetric(vertical: 4),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        child: Row(
          children: [
            Icon(widget.icono, color: colors.primary),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    widget.titulo,
                    style: Theme.of(context).textTheme.titleSmall,
                  ),
                  Text(
                    widget.subtitulo,
                    style: Theme.of(
                      context,
                    ).textTheme.bodySmall?.copyWith(color: colors.outline),
                  ),
                ],
              ),
            ),
            if (_cargando)
              const Padding(
                padding: EdgeInsets.symmetric(horizontal: 12),
                child: SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2),
                ),
              )
            else ...[
              IconButton(
                tooltip: 'Ver',
                onPressed: _ver,
                icon: const Icon(Icons.visibility_outlined),
              ),
              IconButton(
                tooltip: 'Descargar',
                onPressed: _descargar,
                icon: const Icon(Icons.download_outlined),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Visor de PDF a pantalla completa (también lo usa Mis Cuotas para el ticket).
class VisorPdfScreen extends StatelessWidget {
  const VisorPdfScreen({super.key, required this.bytes, required this.titulo});

  final Uint8List bytes;
  final String titulo;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(titulo)),
      body: PdfPreview(
        build: (format) => bytes,
        canChangeOrientation: false,
        canChangePageFormat: false,
        canDebug: false,
      ),
    );
  }
}
