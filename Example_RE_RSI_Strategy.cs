#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;

// PASO 1: Importar el namespace donde reside la colección de indicadores
using NinjaTrader.NinjaScript.Indicators.RuleExtraction;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    /// <summary>
    /// Estrategia de ejemplo que demuestra cómo integrar y utilizar el indicador RE_RSI
    /// (perteneciente a la colección RuleExtraction) dentro de una estrategia NinjaScript oficial.
    /// </summary>
    public class Example_RE_RSI_Strategy : Strategy
    {
        // PASO 2: Declarar la variable privada para almacenar la instancia del indicador
        private RE_RSI rsiIndicator;

        #region Inputs y Parámetros Configurables
        [NinjaScriptProperty]
        [Range(2, 200)]
        [Display(Name="RsiPeriod", Description="Periodo de cálculo para el RSI de Wilder", Order=1, GroupName="Parámetros RSI")]
        public int RsiPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1.0, 50.0)]
        [Display(Name="OversoldLevel", Description="Nivel de sobreventa para compras", Order=2, GroupName="Parámetros RSI")]
        public double OversoldLevel { get; set; }

        [NinjaScriptProperty]
        [Range(50.0, 99.0)]
        [Display(Name="OverboughtLevel", Description="Nivel de sobrecompra para ventas", Order=3, GroupName="Parámetros RSI")]
        public double OverboughtLevel { get; set; }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                 = "Ejemplo práctico de uso del indicador RE_RSI de RuleExtraction.";
                Name                        = "Example_RE_RSI_Strategy";
                Calculate                   = Calculate.OnBarClose;
                EntriesPerDirection         = 1;
                EntryHandling               = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy= true;
                ExitOnSessionCloseSeconds   = 30;
                OrderFillResolution         = OrderFillResolution.Standard;
                Slippage                    = 0;
                StartBehavior               = StartBehavior.WaitUntilFlat;
                TimeInForce                 = TimeInForce.Gtc;

                // Valores por defecto
                RsiPeriod       = 14;
                OversoldLevel   = 30.0;
                OverboughtLevel = 70.0;
                BarsRequiredToTrade = 20;
            }
            else if (State == State.DataLoaded)
            {
                // PASO 3: Instanciar e inicializar el indicador mediante el método estándar de NinjaScript
                rsiIndicator = RE_RSI(RsiPeriod);
            }
        }

        protected override void OnBarUpdate()
        {
            // PASO 4: Control de calentamiento (esperar a tener suficientes barras para el periodo)
            if (CurrentBar < RsiPeriod + 1)
                return;

            // PASO 5: Lectura de valores del indicador
            // rsiIndicator[0] -> Valor en la vela actual que acaba de cerrar
            // rsiIndicator[1] -> Valor en la vela anterior
            double currentRsi  = rsiIndicator[0];
            double previousRsi = rsiIndicator[1];

            // Si el indicador aún tiene valores indefinidos (NaN), no operar
            if (double.IsNaN(currentRsi) || double.IsNaN(previousRsi))
                return;

            // PASO 6: Lógica operativa de ejemplo (Reversión a la media con cruce de niveles)
            
            // Condición LONG: El RSI sale de la zona de sobreventa cruzando hacia arriba
            bool buySignal = (previousRsi < OversoldLevel && currentRsi >= OversoldLevel);

            // Condición SHORT: El RSI sale de la zona de sobrecompra cruzando hacia abajo
            bool sellSignal = (previousRsi > OverboughtLevel && currentRsi <= OverboughtLevel);

            // Condición de Salida LONG: El RSI recupera el nivel neutral (50) o alcanza sobrecompra
            bool exitLongSignal = (currentRsi >= 50.0);

            // Condición de Salida SHORT: El RSI recupera el nivel neutral (50) o alcanza sobreventa
            bool exitShortSignal = (currentRsi <= 50.0);

            // --- Ejecución de Órdenes ---

            // Gestión de salidas
            if (Position.MarketPosition == MarketPosition.Long && exitLongSignal)
            {
                ExitLong("ExitLong", "LongEntry");
            }
            else if (Position.MarketPosition == MarketPosition.Short && exitShortSignal)
            {
                ExitShort("ExitShort", "ShortEntry");
            }

            // Gestión de entradas
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                if (buySignal)
                {
                    EnterLong("LongEntry");
                }
                else if (sellSignal)
                {
                    EnterShort("ShortEntry");
                }
            }
        }
    }
}
