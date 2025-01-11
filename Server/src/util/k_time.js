export function KSleep(ms)
{
    return new Promise(resolve => setTimeout(resolve, ms));
}
