[<RequireQualifiedAccess>]
module MetricsComparisonChart

open Elmish

open Feliz
open Feliz.ElmishComponents

open Types
open Recharts

type Metric =
    | Tests
    | TotalTests
    | PositiveTests
    | TotalPositiveTests
    | Hospitalized
    | HospitalizedIcu
    | OutOfHospital
    | OutOfHospitalToDate
    | RecoveredToDate
    | Deaths
    | TotalDeaths

type MetricCfg = {
    Metric: Metric
    Color : string
    Visible : bool
    Label : string
}

type Metrics = MetricCfg list

module Metrics  =
    let initial = [
        { Metric = Tests;               Color = "#2B6A7A" ; Visible = false ; Label = "Testiranja" }
        { Metric = TotalTests;          Color = "#74A4AE" ; Visible = false ; Label = "Testiranja - skupaj" }
        { Metric = PositiveTests;       Color = "#7B7226" ; Visible = false ; Label = "Potrjeno okuženi" }
        { Metric = TotalPositiveTests;  Color = "#B0AA74" ; Visible = true  ; Label = "Potrjeno okuženi - skupaj" }
        { Metric = Hospitalized;        Color = "#D09D93" ; Visible = true  ; Label = "Hospitalizirani" }
        { Metric = HospitalizedIcu;     Color = "#B35D4C" ; Visible = true  ; Label = "Intenzivna nega" }
        { Metric = OutOfHospital;       Color = "#52AE73" ; Visible = false ; Label = "Odpuščeni iz bolnišnice" }
        { Metric = OutOfHospitalToDate; Color = "#8CCDAA" ; Visible = false ; Label = "Odpuščeni iz bolnišnice - skupaj" }
        { Metric = RecoveredToDate;     Color = "#D7EEE3" ; Visible = true  ; Label = "Ozdraveli - skupaj" }
        { Metric = Deaths;              Color = "#000000" ; Visible = false ; Label = "Umrli" }
        { Metric = TotalDeaths;         Color = "#666666" ; Visible = true  ; Label = "Umrli - skupaj" }
    ]
    /// find a metric in the list and apply provided function to modify its value
    let update (fn: MetricCfg -> MetricCfg) metric metrics =
        metrics
        |> List.map (fun mc -> if mc.Metric = metric then fn mc else mc)

type State =
    { ScaleType : ScaleType
      Data : StatsData
      Metrics : Metrics }

type Msg =
    | ToggleMetricVisible of Metric
    | ScaleTypeChanged of ScaleType

let init data : State * Cmd<Msg> =
    let state = {
        ScaleType = Linear
        Data = data
        Metrics = Metrics.initial
    }
    state, Cmd.none

let update (msg: Msg) (state: State) : State * Cmd<Msg> =
    match msg with
    | ToggleMetricVisible metric ->
        { state with
            Metrics = state.Metrics |> Metrics.update (fun mc -> { mc with Visible = not mc.Visible}) metric
        }, Cmd.none
    | ScaleTypeChanged scaleType ->
        { state with ScaleType = scaleType }, Cmd.none

let renderChart scaleType (data : StatsData) (metrics : Metrics) =

    let renderLineLabel (input: ILabelProperties) =
        Html.text [
            prop.x(input.x)
            prop.y(input.y)
            prop.fill color.black
            prop.textAnchor.middle
            prop.dy(-10)
            prop.fontSize 10
            prop.text input.value
        ]

    let renderMetric (metric : MetricCfg) (dataKey : StatsDataPoint -> int option) =
        Recharts.line [
            line.name metric.Label
            line.monotone
            line.isAnimationActive false
            line.stroke metric.Color
            line.strokeWidth 2
            line.label renderLineLabel
            line.dataKey dataKey
        ]

    let children =
        seq {
            // when xAxis getx too crowded, set [ xAxis.interval 1 ]
            yield Recharts.xAxis [ xAxis.dataKey (fun point -> Utils.formatChartAxixDate point.Date); xAxis.padding (0,10,0,0); xAxis.interval 0 ]

            let yAxisPropsDefaut = [ ]
            match scaleType with
            | Log ->
                yield Recharts.yAxis (yAxisPropsDefaut @ [yAxis.scale ScaleType.Log ; yAxis.domain (domain.auto, domain.auto); yAxis.padding (16,0,0,0) ])
            | _ ->
                yield Recharts.yAxis yAxisPropsDefaut

            yield Recharts.tooltip [ ]
            yield Recharts.cartesianGrid [ cartesianGrid.strokeDasharray(3, 3) ]

            let maxOption a b =
                match a, b with
                | None, None -> None
                | Some x, None -> Some x
                | None, Some y -> Some y
                | Some x, Some y -> Some (max x y)

            yield!
                metrics
                |> List.filter (fun mc -> mc.Visible)
                |> List.map (fun mc ->
                    let pointData =
                        fun point ->
                            match mc.Metric with
                            | Tests -> maxOption point.Tests point.TestsAt14.Performed |> Utils.zeroToNone
                            | TotalTests -> maxOption point.TotalTests point.TestsAt14.PerformedToDate |> Utils.zeroToNone
                            | PositiveTests -> maxOption point.PositiveTests point.TestsAt14.Positive |> Utils.zeroToNone
                            | TotalPositiveTests -> maxOption point.TotalPositiveTests point.TestsAt14.PositiveToDate |> Utils.zeroToNone
                            | Hospitalized -> point.Hospitalized |> Utils.zeroToNone
                            | HospitalizedIcu -> point.HospitalizedIcu |> Utils.zeroToNone
                            | OutOfHospital -> point.OutOfHospital |> Utils.zeroToNone
                            | OutOfHospitalToDate -> point.OutOfHospitalToDate |> Utils.zeroToNone
                            | RecoveredToDate -> point.RecoveredToDate |> Utils.zeroToNone
                            | Deaths -> point.Deaths |> Utils.zeroToNone
                            | TotalDeaths -> point.TotalDeaths |> Utils.zeroToNone
                    renderMetric mc pointData
                )
        }

    Recharts.lineChart [
        lineChart.data data
        lineChart.children (Seq.toList children)
    ]

let renderChartContainer scaleType data metrics =
    Recharts.responsiveContainer [
        responsiveContainer.width (length.percent 100)
        responsiveContainer.height 450
        responsiveContainer.chart (renderChart scaleType data metrics)
    ]

let renderMetricSelector (metric : MetricCfg) dispatch =
    let style =
        if metric.Visible
        then [ style.backgroundColor metric.Color ; style.borderColor metric.Color ]
        else [ ]
    Html.div [
        prop.onClick (fun _ -> ToggleMetricVisible metric.Metric |> dispatch)
        prop.className [ true, "btn  btn-sm metric-selector"; metric.Visible, "metric-selector--selected" ]
        prop.style style
        prop.text metric.Label ]

let renderMetricsSelectors metrics dispatch =
    Html.div [
        prop.className "metrics-selectors"
        prop.children [
            for mc in metrics do
                yield renderMetricSelector mc dispatch
        ]
    ]

let render state dispatch =
    Html.div [
        Utils.renderScaleSelector state.ScaleType (ScaleTypeChanged >> dispatch)
        renderChartContainer state.ScaleType state.Data state.Metrics
        renderMetricsSelectors state.Metrics dispatch
    ]

type Props = {
    data : StatsData
}

let metricsComparisonChart (props : Props) =
    React.elmishComponent("MetricsComparisonChart", init props.data, update, render)
