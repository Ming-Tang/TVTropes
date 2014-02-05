module TVTropes.Timing
open System
open System.Diagnostics

type private Progress = { mutable sw : Stopwatch
                          mutable prevTime : TimeSpan
                          mutable totalCount : int
                          mutable count : int }

let private prog = { sw = null
                     prevTime = TimeSpan.Zero
                     totalCount = 0
                     count = 1 }

let mutable private logTaskName = ""
let mutable private logSW = null

/// Begin logging
let beginLog name =
  logTaskName <- name
  logSW <- Stopwatch.StartNew()
  printfn "+ %s" logTaskName

/// End logging
let endLog() =
  let t1 = logSW.Elapsed
  logSW.Stop()
  printfn "- [%s] %s" (logSW.Elapsed.ToString()) logTaskName

/// Prints out the task to be performed and the time after it is finished
let logged name task =
  beginLog name
  let v = task()
  endLog()
  v

/// Get the total count of progress
let getProgressCount() =
  prog.totalCount

/// Begin tracking progress with n items
let beginProgress tc =
  prog.sw <- Stopwatch.StartNew()
  prog.prevTime <- prog.sw.Elapsed
  prog.totalCount <- tc
  prog.count <- 0
  ()

/// Advance progress by n
let progress n =
  let elapsed = prog.sw.Elapsed
  let dt = elapsed - prog.prevTime
  prog.count <- prog.count + n

  if dt.TotalSeconds >= 1.0 || prog.count = n || prog.count >= prog.totalCount then
    let percents = float(prog.count * 100) / float(prog.totalCount)
    let etc = (elapsed.TotalMinutes) / (percents) * 100.0
    let etl = etc - elapsed.TotalMinutes

    Console.SetCursorPosition(0, Console.CursorTop)
    printf "Progress: %.2f%% (%d/%d) [elapsed %A; ETL %02d:%02d]     "
            percents
            prog.count prog.totalCount
            elapsed (int etl) (int (60.0 * (etl - floor etl)))
    prog.prevTime <- elapsed

  if prog.count >= prog.totalCount then
    printfn ""
    prog.sw.Stop()
