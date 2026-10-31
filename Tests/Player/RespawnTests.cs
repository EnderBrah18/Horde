using System;
static class RespawnTests
{
    static int checks;
    static void Check(bool condition,string message) {checks++;if(!condition)throw new Exception(message);}
    static void Main()
    {
        var timer=new PlayerRespawnCountdown();
        Check(!timer.Running && timer.Remaining==0,"new run has no pending respawn");
        Check(timer.Start(10) && timer.Running && timer.Remaining==10,"death starts ten seconds");
        Check(!timer.Start(100) && timer.Remaining==10,"duplicate death cannot restart countdown");
        Check(!timer.Tick(3,false) && timer.Remaining==7,"simulation ticks without early respawn");
        Check(!timer.Tick(0,false) && timer.Remaining==7,"paused time cannot advance countdown");
        Check(!timer.Tick(-10,false) && timer.Remaining==7,"negative delta cannot add wait time");
        Check(timer.Tick(7,false) && !timer.Running && timer.Remaining==0,"completion emits once after ten seconds");
        Check(!timer.Tick(10,false),"completion cannot emit duplicate revival");
        Check(timer.Start(10),"next death starts a fresh countdown");
        timer.Tick(9,false);
        Check(!timer.Tick(1,true) && !timer.Running && timer.Remaining==0,"core destruction wins over simultaneous countdown completion");
        Check(!timer.Tick(100,false),"cancelled countdown cannot revive after game over");
        timer.Start(0);
        Check(timer.Tick(0,false),"zero configured duration supports immediate completion");
        timer.Start(10);timer.Cancel();
        Check(!timer.Running && timer.Remaining==0,"explicit cancellation clears countdown");
        Console.WriteLine("PASS: "+checks+" respawn countdown checks");
    }
}
