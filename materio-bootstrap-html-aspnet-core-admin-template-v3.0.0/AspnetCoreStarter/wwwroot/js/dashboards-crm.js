/**
 * Analytics Cards
 */

'use strict';
(function () {
  let cardColor,
    creditCard,
    expiryDateMask,
    cvvMask,
    headingColor,
    labelColor,
    fontFamily,
    borderColor,
    heatMap1,
    heatMap2,
    heatMap3,
    heatMap4,
    bodyColor,
    currentTheme,
    chartBgColor;

  if (isDarkStyle) {
    heatMap1 = '#333457';
    heatMap2 = '#3c3e75';
    heatMap3 = '#484b9b';
    heatMap4 = '#696cff';
    chartBgColor = '#474360';
    currentTheme = 'dark';
  } else {
    heatMap1 = '#ededff';
    heatMap2 = '#d5d6ff';
    heatMap3 = '#b7b9ff';
    heatMap4 = '#696cff';
    chartBgColor = '#F0F2F8';
    currentTheme = 'light';
  }
  cardColor = config.colors.cardColor;
  headingColor = config.colors.headingColor;
  labelColor = config.colors.textMuted;
  borderColor = config.colors.borderColor;
  bodyColor = config.colors.bodyColor;
  fontFamily = config.fontFamily;

  // Chart Colors
  const chartColors = {
    donut: {
      series1: config.colors.primary,
      series2: '#9055fdb3',
      series3: '#9055fd80'
    },
    donut2: {
      series1: '#49AC00',
      series2: '#4DB600',
      series3: config.colors.success,
      series4: '#78D533',
      series5: '#9ADF66',
      series6: '#BBEA99'
    },
    line: {
      series1: config.colors.warning,
      series2: config.colors.primary,
      series3: '#7367f029'
    }
  };

  // Total Sales Line Chart
  // --------------------------------------------------------------------
  const totalSalesChartEl = document.querySelector('#totalSalesChart'),
    totalSalesChartConfig = {
      chart: {
        type: 'line',
        height: 230,
        parentHeightOffset: 0,
        toolbar: {
          show: false
        }
      },
      series: [
        {
          data: [0, 90, 10, 80, 50, 130]
        }
      ],
      tooltip: {
        enabled: false
      },
      stroke: {
        curve: 'smooth',
        width: 5,
        lineCap: 'round'
      },
      legend: {
        show: false
      },
      grid: {
        show: false,
        padding: {
          top: -15
        }
      },
      colors: [config.colors.success],
      fill: {
        type: 'gradient',
        gradient: {
          shade: 'dark',
          gradientToColors: [cardColor],
          shadeIntensity: 1,
          inverseColors: false,
          type: 'horizontal',
          opacityFrom: 1,
          opacityTo: 1,
          stops: [0, 100]
        }
      },
      xaxis: {
        categories: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul'],
        labels: {
          style: {
            colors: bodyColor,
            fontFamily: fontFamily,
            fontSize: '0.937rem',
            fontWeight: 400
          }
        },
        axisBorder: {
          show: false
        },
        axisTicks: {
          show: false
        }
      },
      yaxis: {
        show: false
      }
    };

  if (typeof totalSalesChartEl !== undefined && totalSalesChartEl !== null) {
    const totalSalesChart = new ApexCharts(totalSalesChartEl, totalSalesChartConfig);
    totalSalesChart.render();
  }
  // Revenue Report Chart
  // --------------------------------------------------------------------
  const revenueReportChartEl = document.querySelector('#revenueReportChart'),
    revenueReportChartOptions = {
      series: [
        {
          name: 'Earning',
          data: [7, 10, 18, 16, 7, 5, 10, 14, 4]
        },
        {
          name: 'Expense',
          data: [-9, -7, -5, -12, -8, -4, -5, -5, -8]
        }
      ],
      chart: {
        height: 225,
        parentHeightOffset: 0,
        stacked: true,
        type: 'bar',
        toolbar: { show: false }
      },
      tooltip: {
        enabled: false
      },
      legend: {
        show: true,
        position: 'bottom',
        offsetY: 10,
        markers: {
          size: 5,
          radius: 12,
          strokeWidth: 0,
          shape: 'circle'
        },
        itemMargin: {
          horizontal: 15,
          vertical: 5
        },
        fontSize: '15px',
        fontFamily: fontFamily,
        fontWeight: 400,
        labels: {
          colors: bodyColor,
          useSeriesColors: false
        }
      },
      plotOptions: {
        bar: {
          horizontal: false,
          columnWidth: '40%', // Increase column width to create more space
          borderRadius: 5,
          startingShape: 'rounded',
          endingShape: 'rounded',
          borderRadiusApplication: 'around'
        }
      },
      colors: [config.colors.success, config.colors.secondary],
      dataLabels: {
        enabled: false
      },
      stroke: {
        curve: 'smooth',
        width: 2,
        lineCap: 'round',
        colors: [cardColor]
      },
      grid: {
        show: false,
        padding: {
          top: -10,
          bottom: -30,
          left: -13,
          right: -5
        }
      },
      fill: {
        opacity: [1, 1]
      },
      xaxis: {
        labels: {
          show: false
        },
        axisTicks: {
          show: false
        },
        axisBorder: {
          show: false
        },
        // Add spacing between categories on x-axis
        categories: [0, 1, 2, 3, 4, 5, 6, 7, 8],
        tickAmount: 9, // Adjust tick amount for spacing
        min: 0,
        max: 8
      },
      yaxis: {
        labels: {
          show: false
        }
      },
      responsive: [
        {
          breakpoint: 1197,
          options: {
            chart: {
              height: 280
            }
          }
        },
        {
          breakpoint: 783,
          options: {
            chart: {
              height: 250
            }
          }
        },
        {
          breakpoint: 768,
          options: {
            plotOptions: {
              bar: {
                columnWidth: '40%' // Adjust for smaller screen sizes
              }
            }
          }
        },
        {
          breakpoint: 520,
          options: {
            chart: {
              height: 200
            }
          }
        }
      ],
      states: {
        hover: {
          filter: {
            type: 'none'
          }
        },
        active: {
          filter: {
            type: 'none'
          }
        }
      }
    };
  if (typeof revenueReportChartEl !== undefined && revenueReportChartEl !== null) {
    const revenueReportChart = new ApexCharts(revenueReportChartEl, revenueReportChartOptions);
    revenueReportChart.render();
  }

  // Sales Overview Chart
  // --------------------------------------------------------------------
  const salesOverviewChartE1 = document.querySelector('#salesOverviewChart'),
    salesOverviewChartConfig = {
      chart: {
        height: 240,
        parentHeightOffset: 0,
        type: 'donut'
      },
      labels: ['Apparel', 'Electronics', 'FMCG', 'Other Sales'],
      series: [12, 25, 13, 50],
      colors: [chartColors.donut.series1, chartColors.donut.series2, chartColors.donut.series3, chartBgColor],
      stroke: {
        width: 0
      },
      dataLabels: {
        enabled: false
      },
      legend: {
        show: false
      },
      tooltip: {
        style: {
          color: config.colors.danger
        },
        theme: currentTheme
      },
      grid: {
        padding: {
          top: 15
        }
      },
      plotOptions: {
        pie: {
          donut: {
            size: '70%',
            labels: {
              show: true,
              value: {
                fontSize: '24px',
                fontFamily: fontFamily,
                color: headingColor,
                fontWeight: 500,
                offsetY: -20,
                formatter: function (val) {
                  return parseInt(val) + 'k';
                }
              },
              name: {
                offsetY: 20,
                fontFamily: fontFamily,
                color: bodyColor
              },
              total: {
                show: true,
                fontSize: '15px',
                fontFamily: fontFamily,
                label: 'Weekly Visits',
                color: bodyColor,
                formatter: function (w) {
                  return '102k';
                }
              }
            }
          }
        }
      },
      responsive: [
        {
          breakpoint: 1399,
          options: {
            chart: {
              height: 200
            }
          }
        },
        {
          breakpoint: 420,
          options: {
            chart: {
              height: 300
            }
          }
        }
      ]
    };
  if (typeof salesOverviewChartE1 !== undefined && salesOverviewChartE1 !== null) {
    const salesOverviewChart = new ApexCharts(salesOverviewChartE1, salesOverviewChartConfig);
    salesOverviewChart.render();
  }
  // Weekly Sales Chart
  // --------------------------------------------------------------------
  const weeklySalesChartEl = document.querySelector('#weeklySalesChart'),
    weeklySalesChartConfig = {
      chart: {
        height: 250,
        type: 'bar',
        parentHeightOffset: 0,
        toolbar: {
          show: false
        }
      },
      plotOptions: {
        bar: {
          borderRadius: 4,
          distributed: true,
          columnWidth: '60%',
          endingShape: 'rounded',
          startingShape: 'rounded'
        }
      },
      series: [
        {
          data: [38, 55, 48, 65, 80, 38, 48]
        }
      ],
      tooltip: {
        enabled: false
      },
      legend: {
        show: false
      },
      dataLabels: {
        enabled: false
      },
      colors: [
        config.colors_label.primary,
        config.colors_label.primary,
        config.colors_label.primary,
        config.colors_label.primary,
        config.colors.primary,
        config.colors_label.primary,
        config.colors_label.primary
      ],
      grid: {
        show: false,
        padding: {
          top: -15,
          left: -7,
          right: -4
        }
      },
      states: {
        hover: {
          filter: {
            type: 'none'
          }
        },
        active: {
          filter: {
            type: 'none'
          }
        }
      },
      xaxis: {
        axisTicks: {
          show: false
        },
        axisBorder: {
          show: false
        },
        categories: ['S', 'M', 'T', 'W', 'T', 'F', 'S'],
        labels: {
          style: {
            colors: bodyColor
          }
        }
      },
      yaxis: { show: false },
      responsive: [
        {
          breakpoint: 1025,
          options: {
            chart: {
              height: 210
            }
          }
        }
      ]
    };
  if (typeof weeklySalesChartEl !== undefined && weeklySalesChartEl !== null) {
    const weeklySalesChart = new ApexCharts(weeklySalesChartEl, weeklySalesChartConfig);
    weeklySalesChart.render();
  }

  // Total Growth chart
  // --------------------------------------------------------------------
  const totalGrowthAreaChartEl = document.querySelector('#totalGrowthAreaChart'),
    totalGrowthAreaChartConfig = {
      chart: {
        height: 100,
        type: 'area',
        parentHeightOffset: 0,
        toolbar: {
          show: false
        }
      },
      grid: {
        show: false,
        xaxis: {
          lines: {
            show: false
          }
        },
        yaxis: {
          lines: {
            show: false
          }
        },
        padding: {
          top: -15,
          left: -7,
          right: 9,
          bottom: -15
        }
      },
      colors: [config.colors.success],
      dataLabels: {
        enabled: false
      },
      stroke: {
        width: 3,
        curve: 'smooth'
      },
      series: [
        {
          data: [10, 25, 20, 40, 24, 50, 42]
        }
      ],
      tooltip: {
        shared: false,
        intersect: true,
        x: {
          show: false
        }
      },
      xaxis: {
        labels: {
          show: false
        },
        axisTicks: {
          show: false
        },
        axisBorder: {
          show: false
        }
      },
      yaxis: {
        labels: {
          show: false
        }
      },
      tooltip: {
        enabled: false
      },
      fill: {
        type: 'gradient',
        gradient: {
          opacityTo: 0.7,
          opacityFrom: 0.5,
          shadeIntensity: 1,
          stops: [0, 90, 100],
          colorStops: [
            [
              {
                offset: 0,
                opacity: 0.6,
                color: config.colors.success
              },
              {
                offset: 100,
                opacity: 0.1,
                color: cardColor
              }
            ]
          ]
        }
      },
      responsive: [
        {
          breakpoint: 1350,
          options: {
            chart: {
              height: 80
            }
          }
        },
        {
          breakpoint: 1200,
          options: {
            chart: {
              height: 100
            }
          }
        },
        {
          breakpoint: 992,
          options: {
            chart: {
              height: 100
            }
          }
        },
        {
          breakpoint: 768,
          options: {
            chart: {
              height: 110
            }
          }
        }
      ]
    };
  if (typeof totalGrowthAreaChartEl !== undefined && totalGrowthAreaChartEl !== null) {
    const totalGrowthAreaChart = new ApexCharts(totalGrowthAreaChartEl, totalGrowthAreaChartConfig);
    totalGrowthAreaChart.render();
  }

  // Credit Card Validation
  // --------------------------------------------------------------------

  const creditCardPayment = document.querySelector('.credit-card-payment'),
    expiryDatePayment = document.querySelector('.expiry-date-payment'),
    cvvMaskList = document.querySelectorAll('.cvv-code-payment');

  // Credit Card Cleave Masking
  if (creditCard) {
    creditCard.addEventListener('input', event => {
      creditCard.value = formatCreditCard(event.target.value);
      const cleanValue = event.target.value.replace(/\D/g, '');
      let cardType = getCreditCardType(cleanValue);
      if (cardType && cardType !== 'unknown' && cardType !== 'general') {
        document.querySelector('.card-type').innerHTML =
          `<img src="${assetsPath}img/icons/payments/${cardType}-cc.png" height="28"/>`;
      } else {
        document.querySelector('.card-type').innerHTML = '';
      }
    });
    registerCursorTracker({
      input: creditCard,
      delimiter: ' '
    });
  }

  // Expiry Date Mask
  if (expiryDateMask) {
    expiryDateMask.addEventListener('input', event => {
      expiryDateMask.value = formatDate(event.target.value, {
        date: true,
        delimiter: '/',
        datePattern: ['m', 'y']
      });
    });
    registerCursorTracker({
      input: expiryDateMask,
      delimiter: '/'
    });
  }

  // CVV
  if (cvvMask) {
    cvvMask.addEventListener('input', event => {
      const cleanValue = event.target.value.replace(/\D/g, '');
      cvvMask.value = formatNumeral(cleanValue, {
        numeral: true,
        numeralPositiveOnly: true
      });
    });
  }
})();
