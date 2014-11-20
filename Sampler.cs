using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsvPick
{
    public class Sampler
    {
        private Random _rand;
        private double _prob;


        public Sampler( int percent, int seed=0 )
        {
            if( seed == 0 )
                seed = (int)(DateTime.Now.Ticks & 0xFFFFFFFF);
            
            percent = Math.Max( 0, Math.Min( 101, percent ) );
            this._rand = new Random( seed );
            this._prob = percent * 0.01;
        }


        public bool Vote()
        {
            return this._prob >= 1.0 
                    ? true 
                    : this._rand.NextDouble() <= this._prob;
        }
    }



    public static class Extensions
    {
        public static IEnumerable<T>  SampleFrom<T>( this IEnumerable<T> seq, Sampler s )
        {
            foreach( var elem in seq )
            {
                if( s.Vote() ) 
                    yield return elem;
            }
        }
    }
}
