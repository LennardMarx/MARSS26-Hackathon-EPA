
[System.Serializable]
public class TrackingData {

    public float quality { get; private set; }
    public int lastButtonEvent { get; private set; }
    public double timestamp { get; private set; }

    public float positionX;
    public float positionY;
    public float positionZ;
    public float rotationX;
    public float rotationY;
    public float rotationZ;
    public float rotationW;


    public TrackingData(float posx, float posy, float posz, float rotqx, float rotqy, float rotqz, float rotq0, double time, float quality, int lastButtonEvent) {
        this.positionX = posx;
        this.positionY = posy;
        this.positionZ = posz;
        this.rotationX = rotqx;
        this.rotationY = rotqy;
        this.rotationZ = rotqz;
        this.rotationW = rotq0;
        this.quality = quality;
        this.lastButtonEvent = lastButtonEvent;
        this.timestamp = time;
}



    public TrackingData Copy() {
        return (TrackingData)this.MemberwiseClone();
    }
}

