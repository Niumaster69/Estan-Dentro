# Créditos de modelos 3D y animaciones (prólogo)

Descargados el 19-oct-2026 con la cuenta de Sketchfab de Henry (llave en `D:\sketchfab\token.txt`) y su cuenta de Adobe (Mixamo).
**CC-BY obliga a dar crédito**: estos nombres deben salir en los créditos del juego (pantalla de créditos: pendiente).

| Qué es en el juego | Modelo | Autor | Licencia | Enlace |
|---|---|---|---|---|
| El carro de Pa (Corolla blanco, interior completo) | 2014 Toyota Corolla E180 EU (with interior) | armoredwave | CC Attribution | https://sketchfab.com/3d-models/36f95efb0585464cae43a25a3b3392e8 |
| Pa (al volante) | Man Sitting | apexpro | CC Attribution | https://sketchfab.com/3d-models/d8ca320050c6494581c34292356a280b |
| Cuerpo de Mateo (copiloto) | man sitting | Ace-of_spades | CC Attribution | https://sketchfab.com/3d-models/b46bceb164b74346897c7a62691a1d5c |
| El camión del presagio | Chevrolet Jumbo Truck | Medhatelo | CC Attribution | https://sketchfab.com/3d-models/27a0aaa34e7d4ea7877393178d82b01c |
| Bus del tráfico | Colombian Public Transport Transmilenio SITP Bus | MatClark740 | CC Attribution | https://sketchfab.com/3d-models/99af515f3e2d4af39e9c558549b98fb3 |
| Carro del tráfico | Renault Logan 2004 | goooddenis | CC Attribution | https://sketchfab.com/3d-models/e4f9463f6e004b90bb977d12f6375b9c |
| Carro del tráfico | Chevrolet Spark | BHP3D | CC Attribution | https://sketchfab.com/3d-models/70342145bcad4441be1827322a8c8879 |
| Taxi del tráfico (pintado de amarillo) | Kia Picanto MK1 2003-2011 | szymonpasterczyk | CC Attribution | https://sketchfab.com/3d-models/b3c8e76124c641e9ba30015a14929218 |
| Carro del tráfico | 2009_Chevrolet_Aveo | creativelion | CC Attribution | https://sketchfab.com/3d-models/0b2cf39c664043ccbdb5fa0261ae5644 |
| Pose de manejar de Pa | Driving | Mixamo (Adobe) | Uso libre en proyectos (licencia de Mixamo) | https://www.mixamo.com |

Cambios hechos a los modelos (CC-BY permite modificarlos): pose de Mixamo pasada al esqueleto de Pa, silla del conductor corrida
hacia adelante, timón acercado al conductor con su columna estirada, cabeza de Mateo oculta (cámara en primera persona),
ropa sin brillo especular y de dos caras en Unity.

Archivos: `02_Arte3D_Blender/prologo/Prologo_Auto.blend` (final), `Prologo_Auto_pose_Pa.blend` (con esqueleto editable),
`Mixamo_Driving.fbx` (la animación).

Vehículos: convertidos con `02_Arte3D_Blender/scripts_blender/prologo_vehiculos.py` (tamaño real, texturas a 2048). Originales en `02_Arte3D_Blender/prologo/vehiculos/`.
Mateo: 92 % de tamaño, recostado, rodillas y pies doblados, torso comprimido 12 % (`prologo_mateo_sentado.py`, `prologo_mateo_cojin.py`, `prologo_mateo_torso.py`). Palanca de cambios pasada a D (`prologo_palanca_d.py`).
