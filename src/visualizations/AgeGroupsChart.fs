[<RequireQualifiedAccess>]
module AgeGroupsChart

open Feliz

open Types
open Recharts

let renderChart (data : StatsData) =
    let ageGroupData =
        data
        |> List.rev
        |> List.pick (fun dataPoint ->
            dataPoint.AgeGroups
            |> List.filter (fun ageGroup -> // keep non-empty
                match ageGroup.TestedPositiveMale, ageGroup.TestedPositiveFemale, ageGroup.TestedPositiveAll with
                | None, None, None -> false
                | _ -> true)
            |> function // take most recent day with some data
                | [] -> None
                | filtered -> Some filtered
        )

    Recharts.barChart [
        barChart.data ageGroupData
        barChart.maxBarSize 40
        barChart.barCategoryGapPercentage 15
        barChart.children [
            Recharts.cartesianGrid [ cartesianGrid.strokeDasharray(3, 3) ]
            Recharts.xAxis [ xAxis.dataKey (fun (point : AgeGroup) ->
                match point.AgeFrom, point.AgeTo with
                | None, None -> ""
                | None, Some b -> sprintf "0-%d" b
                | Some a, Some b -> sprintf "%d-%d" a b
                | Some a, None -> sprintf "nad %d" a ) ]
            Recharts.yAxis [ ]

            Recharts.tooltip [ ]
            Recharts.legend [ ]

            Recharts.bar [
                bar.name "Vsi"
                bar.fill "#666666"
                bar.dataKey (fun (point : AgeGroup) -> point.TestedPositiveAll |> Option.defaultValue 0)
            ]

            Recharts.bar [
                bar.name "Ženske"
                bar.fill "#2B6A7A"
                bar.dataKey (fun (point : AgeGroup) -> point.TestedPositiveFemale |> Option.defaultValue 0)
            ]

            Recharts.bar [
                bar.name "Moški"
                bar.fill "#7B7226"
                bar.dataKey (fun (point : AgeGroup) -> point.TestedPositiveMale |> Option.defaultValue 0)
            ]
        ] ]

let render data =
    Recharts.responsiveContainer [
        responsiveContainer.width (length.percent 100)
        responsiveContainer.height 450
        responsiveContainer.chart (renderChart data) ]
