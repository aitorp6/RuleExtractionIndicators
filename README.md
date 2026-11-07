# Guía de Instalación y Uso: Indicadores RuleExtraction para NinjaTrader 8

Esta guía documenta el proceso paso a paso para instalar la colección de indicadores **RuleExtraction** en NinjaTrader 8 y cómo integrarlos fácilmente dentro de cualquier estrategia automática mediante NinjaScript.

---

## 1. Contenido del Paquete

La suite de indicadores está optimizada para trading algorítmico y replica con exactitud matemática (tolerancia $< 10^{-10}$) los cálculos de Python y MQL5:

| Indicador | Descripción |
| :--- | :--- |
| **`RE_Momentum`** | Momentum clásico de precio con periodo configurable. |
| **`RE_WPR`** | Williams' Percent Range (%R). |
| **`RE_Stoch`** | Oscilador Estocástico Rápido / Lento (%K, %D). |
| **`RE_DeMarker`** | Oscilador DeMarker (DeM). |
| **`RE_CloseSmaDev`** | Desviación porcentual del precio de cierre respecto a su media SMA. |
| **`RE_AvIBS`** | Internal Bar Strength Suavizado (Media móvil del IBS). |
| **`RE_KER`** | Kaufman Efficiency Ratio (Ratio de eficiencia direccional vs ruido). |
| **`RE_HBM`** | High Bar Momentum (Distancia y fuerza del máximo reciente). |
| **`RE_LBM`** | Low Bar Momentum (Distancia y fuerza del mínimo reciente). |
| **`RE_RSI`** | Relative Strength Index con suavizado estándar de Wilder. |
| **`RE_TRIX`** | Triple Exponential Average (Oscilador de tasa de cambio TRIX). |
| **`RE_DIWilder`** | Wilder's Directional Indicators (+DI y -DI). |
| **`RE_CCI`** | Commodity Channel Index con desviación media absoluta. |
| **`RE_RVI`** | Relative Vigor Index. |
| **`RE_Aroon`** | Oscilador Aroon (Aroon Up, Aroon Down y Aroon Oscillator). |

---

## 2. Instalación en NinjaTrader 8

Para instalar el archivo `.zip` (*NinjaScript Add-On*) en cualquier equipo con NinjaTrader 8:

1. Inicia **NinjaTrader 8**.
2. En la ventana principal (*Control Center*), dirígete al menú superior:
   ```text
   Tools -> Import -> NinjaScript Add-On...
   ```
3. Aparecerá un aviso de seguridad advirtiendo sobre código de terceros; pulsa **Aceptar / Sí**.
4. Selecciona el archivo `.zip` que contiene los indicadores.
5. NinjaTrader descomprimirá el contenido en la ruta estándar:
   ```text
   Documents\NinjaTrader 8\bin\Custom\Indicators\RuleExtraction\
   ```
6. Se iniciará una compilación automática. Al finalizar, NinjaTrader mostrará un mensaje confirmando:
   > *"NinjaTrader successfully imported all scripts contained in the NinjaScript Archive File."*

---

## 3. Los 6 Pasos para Utilizar los Indicadores en una Estrategia

Para utilizar cualquiera de estos indicadores dentro de tus estrategias en NinjaScript, sigue este patrón estándar de 6 pasos:

### Paso 1: Importar el espacio de nombres
Al inicio de tu archivo `.cs` de estrategia, añade la referencia al paquete en el bloque `#region Using declarations`:

```csharp
using NinjaTrader.NinjaScript.Indicators.RuleExtraction;
```

---

### Paso 2: Declarar la variable de instancia
Declara el indicador como variable privada dentro de la clase de tu estrategia:

```csharp
public class MiEstrategia : Strategy
{
    private RE_RSI rsiIndicator;
    // ...
```

---

### Paso 3: Instanciar y registrar en `State.DataLoaded`
En el método `OnStateChange()`, dentro del estado `State.DataLoaded`, inicializa el objeto con sus parámetros y regístralo con `AddChartIndicator()` para que NinjaTrader gestione su memoria y dibujado:

```csharp
protected override void OnStateChange()
{
    if (State == State.SetDefaults)
    {
        // Configuración general de la estrategia
        Name        = "MiEstrategiaRSI";
        Calculate   = Calculate.OnBarClose;
    }
    else if (State == State.DataLoaded)
    {
        // Instanciación nativa mediante CacheIndicator de NinjaScript
        rsiIndicator = RE_RSI(14);
    }
}
```

---

### Paso 4: Proteger el periodo de calentamiento (*Warm-up*)
Al principio de `OnBarUpdate()`, debes comprobar que existan suficientes barras históricas para que el indicador pueda calcular su valor sin generar excepciones de índice (`IndexOutOfRangeException`):

```csharp
protected override void OnBarUpdate()
{
    // Esperar a que haya suficientes barras para el periodo calculado
    if (CurrentBar < 15) // Periodo + 1
        return;
```

---

### Paso 5: Leer los valores del indicador
Accede a los valores calculados usando los corchetes de indexación de series temporales (`[0]` representa la vela actual recién cerrada y `[1]` la vela previa). Es buena práctica validar que no sean valores nulos (`NaN`):

```csharp
    // Lectura de la serie principal del indicador
    double currentRsi  = rsiIndicator[0];
    double previousRsi = rsiIndicator[1];

    // Descartar si el indicador aún está calculando
    if (double.IsNaN(currentRsi) || double.IsNaN(previousRsi))
        return;
```

> [!NOTE]
> En indicadores multiserie (como `RE_DIWilder` o `RE_Aroon`), puedes acceder a las series secundarias directamente:
> - `diIndicator.PlusDI[0]` y `diIndicator.MinusDI[0]`
> - `aroonIndicator.AroonUp[0]` y `aroonIndicator.AroonDown[0]`

---

### Paso 6: Lógica operativa y envío de órdenes
Utiliza las variables leídas para disparar tus reglas de entrada y salida al mercado:

```csharp
    // Ejemplo: Reversión a la media en sobreventa / sobrecompra
    bool cruceAlcista  = (previousRsi < 30.0 && currentRsi >= 30.0);
    bool cruceBajista  = (previousRsi > 70.0 && currentRsi <= 70.0);
    bool salidaNeutral = (currentRsi >= 50.0);

    // Salidas
    if (Position.MarketPosition == MarketPosition.Long && salidaNeutral)
    {
        ExitLong("CierreRsi", "EntradaLong");
    }

    // Entradas
    if (Position.MarketPosition == MarketPosition.Flat)
    {
        if (cruceAlcista)
        {
            EnterLong("EntradaLong");
        }
        else if (cruceBajista)
        {
            EnterShort("EntradaShort");
        }
    }
}
```

---

## 4. Código Completo de Ejemplo

Puedes consultar la implementación completa y lista para compilar en:
- [`Example_RE_RSI_Strategy.cs`](file:///C:/Users/USER/Documents/NinjaTrader%208/bin/Custom/Strategies/Example_RE_RSI_Strategy.cs)

---

## 5. Uso Directo en Gráficos de NinjaTrader

Si además de usarlos en estrategias quieres ver los indicadores visualmente en un gráfico (*Chart*):

1. Abre un gráfico en NinjaTrader.
2. Haz clic derecho sobre el gráfico y selecciona **`Indicators...`** (o pulsa `Ctrl + I`).
3. En la lista superior izquierda, busca la carpeta o prefijo **`RE_`** (por ejemplo, `RE_RSI`, `RE_Momentum`, `RE_Aroon`).
4. Haz doble clic en el indicador deseado, ajusta los parámetros en el panel derecho y pulsa **`OK`**.

---

## 6. Solución de Problemas Frecuentes

* **Error de compilación `CS0246: The type or namespace name 'RE_RSI' could not be found`**:
  * *Solución*: Falta la línea `using NinjaTrader.NinjaScript.Indicators.RuleExtraction;` al inicio del archivo.
* **Error `IndexOutOfRangeException` al ejecutar la estrategia**:
  * *Solución*: Asegúrate de incluir la guarda `if (CurrentBar < Period + 1) return;` en `OnBarUpdate()` para evitar leer barras inexistentes durante la primera barra del gráfico.
