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


        public Sampler( double percent, int seed=0 )
        {
            if( seed == 0 )
                seed = (int)(DateTime.Now.Ticks & 0xFFFFFFFF);
            
            // Clamp and bump in case of roundoff
            const double bump = 1.0E-6;
            percent = Math.Max( 0.0, percent );
            if( percent > 0.0 )
                this._prob = Math.Min( 1.0, (0.01 * percent) + bump );

            this._rand = new Random( seed );
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
