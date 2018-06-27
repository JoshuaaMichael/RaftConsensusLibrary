namespace TeamDecided.RaftConsensus.Networking.Enums
{
    internal enum ISRPStep
    {
        NotContacted,
        Step1,
        Step2,
        Step3,
        Step4,
        Step5,
        Complete,
        TimedOut
    }
}
