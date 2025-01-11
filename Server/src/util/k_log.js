export class KLog
{
    static I(TAG, logStr)
    {
        console.log(`[${TAG}] ${logStr}`);
    }

    static W(TAG, logStr)
    {
        console.warn(`[${TAG}] ${logStr}`);
    }

    static E(TAG, logStr)
    {
        console.error(`[${TAG}] ${logStr}`);
    }
}
