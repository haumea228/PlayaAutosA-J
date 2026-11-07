class UserModel {
  const UserModel({
    required this.id,
    required this.email,
    required this.nombre,
    required this.rol,
    this.token,
  });

  final int id;
  final String email;
  final String nombre;
  final String rol; // 'Cliente', 'AdministradorP', etc.
  final String? token;

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      id: json['id'] as int,
      email: json['email'] as String,
      nombre: json['nombre'] as String,
      rol: json['rol'] as String,
      token: json['token'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'email': email,
      'nombre': nombre,
      'rol': rol,
      'token': token,
    };
  }
}
