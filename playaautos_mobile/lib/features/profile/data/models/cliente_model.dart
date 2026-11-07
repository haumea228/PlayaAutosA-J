// Wire format verified against ClienteDto.cs / MiCuentaController.GetPerfil().
// El JSON real serializa CI_RUC como "cI_RUC" (no "ciRuc").
//
// Aviso: rangoPrecioMin/rangoPrecioMax existen en la entidad Cliente de la
// base de datos, pero NINGÚN DTO accesible por un Cliente los incluye —
// ni ClienteDto (GET /api/micuenta/perfil) ni ActualizarPerfilClienteDto
// (PUT /api/micuenta/perfil). Solo se usan al crear un cliente desde el
// admin (CrearClienteDto, AdministradorP/Vendedor-only). Quedan acá como
// nullable porque siempre van a venir null desde este endpoint; no hay
// forma de leerlos ni editarlos desde la app de clientes hoy.
class ClienteModel {
  const ClienteModel({
    required this.id,
    required this.nombre,
    this.ciRuc,
    this.telefono,
    this.email,
    this.direccion,
    this.rangoPrecioMin,
    this.rangoPrecioMax,
    required this.activo,
  });

  final int id;
  final String nombre;
  final String? ciRuc;
  final String? telefono;
  final String? email;
  final String? direccion;
  final int? rangoPrecioMin;
  final int? rangoPrecioMax;
  final bool activo;

  factory ClienteModel.fromJson(Map<String, dynamic> json) {
    return ClienteModel(
      id: json['clienteId'] as int,
      nombre: json['nombre'] as String? ?? '',
      ciRuc: json['cI_RUC'] as String?,
      telefono: json['telefono'] as String?,
      email: json['email'] as String?,
      direccion: json['direccion'] as String?,
      rangoPrecioMin: json['rangoPrecioMin'] as int?,
      rangoPrecioMax: json['rangoPrecioMax'] as int?,
      activo: json['activo'] as bool? ?? true,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'clienteId': id,
      'nombre': nombre,
      'cI_RUC': ciRuc,
      'telefono': telefono,
      'email': email,
      'direccion': direccion,
      'rangoPrecioMin': rangoPrecioMin,
      'rangoPrecioMax': rangoPrecioMax,
      'activo': activo,
    };
  }

  ClienteModel copyWith({
    String? nombre,
    String? telefono,
    String? direccion,
  }) {
    return ClienteModel(
      id: id,
      nombre: nombre ?? this.nombre,
      ciRuc: ciRuc,
      telefono: telefono ?? this.telefono,
      email: email,
      direccion: direccion ?? this.direccion,
      rangoPrecioMin: rangoPrecioMin,
      rangoPrecioMax: rangoPrecioMax,
      activo: activo,
    );
  }
}
