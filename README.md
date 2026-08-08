# ⚡ AñilHeX - Editor Maestro de Partidas para Pokémon Añil

**AñilHeX** es una herramienta de escritorio ligera, rápida e intuitiva desarrollada en **C# (.NET 8)** para la edición y modificación de archivos de guardado (`.rxdata`) de **Pokémon Añil** (v4.0+).

Permite modificar parámetros de los Pokémon en el Equipo y las Cajas del PC, gestionar el inventario de la mochila, editar el dinero del jugador y manejar transformaciones complejas como Megas, formas regionales y el modo Randomizer.

---

## ✨ Características Principales

### 🔴 Editor de Pokémon (Equipo y PC)
* **Visualización en Cuadrícula:** Vista estilo caja para el PC y para el Equipo (2x3) con sprites dinámicos.
* **Arrastrar y Soltar (Drag & Drop):** Mueve Pokémon libremente entre espacios del PC, reorganiza el equipo o arrastra entre pestañas en tiempo real.
* **Edición de Stats Físicas y Ocultas:**
  * Modificación de Nivel, Experiencia calculada automáticamente por tasa de crecimiento y Mote personalizado.
  * Selector de Shiny ⭐ y Radiante (Super Shiny) 🌟.
  * Maximizado instantáneo de Felicidad (255) ♥.
  * Selección de Sexo, Objeto equipado y Naturaleza.
  * Control total de IVs (0-31) y EVs (0-252) con **indicador de color dinámico** según la naturaleza (🔴 Aumento / 🔵 Disminución).
* **Gestor de Movimientos:** Configuración de los 4 ataques, PPs individuales, PPs Max e inyección rápida con botón *Max PPs Todos*.

### 🌀 Módulo de Formas, Megas y Paradojas
* Enciclopedia interna con detección automática de variantes:
  * **Megas Oficiales y de Añil** (Mega Butterfree, Mega Lapras, Mega Machamp, etc.).
  * **Formas Regionales:** Alola, Galar, Hisui y Paldea.
  * **Variantes Especiales:** Formas de Darmanitan (Zen), Terapagos, Ogerpon, Rotom, Deoxys, Lycanroc y cortes de Furfrou.
  * **Conversión a Paradojas:** Grupos de equivalencia para transformar especies a sus contrapartes del pasado/futuro.

### 🎲 Compatibilidad Total con Modo Randomizer
* Incluye el modo **`Auto: Ranura (1/2/Oculta)`**. Al usarlo, el editor libera la habilidad en la partida guardada para que el motor interno de Pokémon Añil calcule y asigne la habilidad aleatoria nativa de esa partida sin corromper el guardado.
* Opción para forzar habilidades personalizadas o explorar la base de datos completa de habilidades.

### 🎒 Gestión del Entrenador e Inventario
* **Mochila Multisección:** Separación por los 8 bolsillos oficiales (Objetos, Medicinas, Poké Balls, MTs/MOs, Bayas, Mega Piedras, Batalla, Clave).
* **Apilamiento Inteligente:** Sistema en cascada que llena huecos existentes hasta 999 y crea nuevos slots automáticamente si se añaden más de 1000 unidades.
* **Protección de Ítems Clave:** Filtro para evitar duplicados en MTs y Objetos Clave.
* **Billetera:** Edición directa del dinero del jugador hasta 9,999,999 ₽.
* **Botón de Borrado de Objetos:** Eliminación limpia de registros en la mochila.

---

## 🚀 Compilación e Instalación

### Requisitos Prácticos
* **Windows 10 / 11** (64-bit).
* **.NET 8.0 SDK** (solo necesario si vas a compilar el código fuente).

### Paso a Paso para Compilar

1. Clona el repositorio:
   ```bash
   git clone [https://github.com/DylanVZ5/AnilHeX.git](https://github.com/DylanVZ5/AnilHeX.git)