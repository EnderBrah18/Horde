using System;

static class ProgressionTests
{
    static int checks;
    static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }
    static void Main()
    {
        Check(VillageProgressionRules.DayNumber(0,false,false)==1,"initial morning is day one");
        Check(VillageProgressionRules.DayNumber(1,true,false)==1,"first night belongs to day one");
        Check(VillageProgressionRules.DayNumber(1,false,false)==2,"first victory advances day");
        Check(VillageProgressionRules.DayNumber(2,true,false)==2,"second night belongs to day two");
        Check(VillageProgressionRules.DayNumber(2,false,false)==3,"second dawn advances once");
        Check(VillageProgressionRules.DayNumber(1,false,true)==1,"defeat does not advance calendar");
        Check(VillageProgressionRules.DayNumber(0,false,true)==1,"early defeat remains day one");
        Check(VillageProgressionRules.WavesForNight(1,3,2,5)==3,"first night has three waves");
        Check(VillageProgressionRules.WavesForNight(2,3,2,5)==3,"second night retains three waves");
        Check(VillageProgressionRules.WavesForNight(3,3,2,5)==4,"third night adds a wave");
        Check(VillageProgressionRules.WavesForNight(5,3,2,5)==5,"fifth night has five waves");
        Check(VillageProgressionRules.WavesForNight(50,3,2,5)==5,"wave cap enforced");
        Check(VillageProgressionRules.WavesForNight(1,0,0,0)==1,"invalid zero balance values handled");
        Check(!VillageProgressionRules.CanDiscover(1,2,false,false,false),"destination hidden on day one");
        Check(!VillageProgressionRules.CanDiscover(2,2,true,false,false),"destination cannot unlock at night");
        Check(!VillageProgressionRules.CanDiscover(2,2,false,true,false),"defeat cannot unlock destination");
        Check(VillageProgressionRules.CanDiscover(2,2,false,false,false),"destination unlocks on second morning");
        Check(!VillageProgressionRules.CanDiscover(2,2,false,false,true),"discovery cannot repeat");
        Check(!VillageProgressionRules.CanDiscover(2,3,false,false,false),"custom discovery day respected");
        Check(VillageProgressionRules.CanDiscover(3,3,false,false,false),"later custom day unlocks");
        var waves = new NightWaveSequence(3,3);
        Check(!waves.TryAdvance(0),"empty arena cannot skip unspawned groups");
        Check(!waves.CanFinishNight(0),"empty arena before first group is not dawn");
        waves.RecordGroupSpawned();
        Check(!waves.TryAdvance(0),"killing first group does not advance the wave");
        waves.RecordGroupSpawned(); waves.RecordGroupSpawned();
        Check(!waves.TryAdvance(1),"one surviving enemy blocks next wave");
        Check(waves.TryAdvance(0) && waves.CurrentWave==2,"all three groups resolved starts second wave");
        Check(waves.SpawnedGroups==0,"next wave has its own group schedule");
        waves.RecordGroupSpawned(); waves.RecordGroupSpawned(); waves.RecordGroupSpawned();
        Check(!waves.CanFinishNight(0),"clearing second wave does not end a three-wave night");
        Check(waves.TryAdvance(0) && waves.CurrentWave==3,"second cleared wave advances to third");
        waves.RecordGroupSpawned(); waves.RecordGroupSpawned();
        Check(!waves.CanFinishNight(0),"final wave must finish spawning all groups");
        waves.RecordGroupSpawned();
        Check(!waves.CanFinishNight(1),"final survivors block dawn");
        Check(waves.CanFinishNight(0),"nine groups cleared across three waves allows dawn");
        Check(!waves.TryAdvance(0),"no fourth wave after configured final wave");
        var varied=new NightWaveSequence(new[]{2,1,3});
        varied.RecordGroupSpawned();
        Check(!varied.TryAdvance(0),"partial variable wave cannot advance");
        varied.RecordGroupSpawned();
        Check(varied.TryAdvance(0) && varied.GroupsPerWave==1,"next wave uses its own batch count");
        varied.RecordGroupSpawned();
        Check(varied.TryAdvance(0) && varied.GroupsPerWave==3,"third wave restores its own batch count");
        varied.RecordGroupSpawned(); varied.RecordGroupSpawned(); varied.RecordGroupSpawned();
        Check(varied.CanFinishNight(0),"variable wave schedule finishes only after final batches");
        Check(VillageProgressionRules.CanTakePersonalExpedition(2,-1),"first personal departure available");
        Check(!VillageProgressionRules.CanTakePersonalExpedition(2,2),"second departure blocked on same day regardless of destination");
        Check(VillageProgressionRules.CanTakePersonalExpedition(3,2),"next morning restores daily personal departure");
        Check(!VillageProgressionRules.CanTakePersonalExpedition(2,3),"backward calendar cannot bypass daily limit");
        Check(!VillageProgressionRules.CanTakePersonalExpedition(0,-1),"invalid calendar cannot depart");
        Console.WriteLine("PASS: "+checks+" progression checks");
    }
}
