class ZoneService
{
    public float greenLower = 2, greenUpper = 20;
    public bool greenBlink = true;

    public float yellowLower = 20, yellowUpper = 40;
    public bool yellowBlink = true;

    public float redLower = 40, redUpper = 60;
    public bool redBlink = false;

    public float blueLower = 60, blueUpper = 80;
    public bool blueBlink = false;

    public int greenState=0, yellowState=0, redState=0, blueState=0;

    public float lastDistance=0.0F;

    public bool Update(float dist)
    {
        int prevG = greenState, prevY = yellowState, prevR = redState, prevB = blueState;

        if(dist>=greenLower && dist<=greenUpper){
            if(greenBlink==true){
                greenState=2;
            }else{greenState=1;}
        }
        else{greenState=0;}
        
        if(dist>=yellowLower && dist<=yellowUpper){
            if(yellowBlink==true){
                yellowState=2;
            }else{yellowState=1;}
        }
        else{yellowState=0;}

        if(dist>=redLower && dist<=redUpper){
            if(redBlink==true){
                redState=2;
            }else{redState=1;}
        }
        else{redState=0;}
        
        if(dist>=blueLower && dist<=blueUpper){
            if(blueBlink==true){
                blueState=2;
            }else{blueState=1;}
        }
        else{blueState=0;}

        lastDistance=dist;
        return prevG != greenState || prevY != yellowState || prevR != redState || prevB != blueState;
    }

    public string GetCommand()
    {
        return $"LED_CONFIG:{greenState},{yellowState},{redState},{blueState}";
    }
}